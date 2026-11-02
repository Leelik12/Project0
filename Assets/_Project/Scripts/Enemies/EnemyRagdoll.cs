using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Рэгдолл, собираемый из кода в момент смерти: на основные кости гуманоида вешаются Rigidbody, коллайдеры
/// и CharacterJoint, Animator выключается, тело отлетает от последнего попадания. Настраивать в префабах ничего не нужно.
/// Уже существующие на костях коллайдеры попаданий (Head/Body/Leg) используются как части тела.
/// </summary>
public static class EnemyRagdoll
{
    private const float TotalMass = 70f;
    private const float HitImpulse = 30f;

    private struct BoneSpec
    {
        public HumanBodyBones Bone;
        public HumanBodyBones Child;   // куда «смотрит» кость — по нему считается капсула
        public float MassShare;
        public float Radius;           // радиус капсулы (сферы для головы), метры
        public float Swing;            // предел сгиба, градусы

        public BoneSpec(HumanBodyBones bone, HumanBodyBones child, float massShare, float radius, float swing)
        {
            Bone = bone;
            Child = child;
            MassShare = massShare;
            Radius = radius;
            Swing = swing;
        }
    }

    // Порядок важен: родитель идёт раньше потомков, чтобы сустав нашёл тело родителя
    private static readonly BoneSpec[] Specs =
    {
        new(HumanBodyBones.Hips, HumanBodyBones.Spine, 0.18f, 0.14f, 0f),
        new(HumanBodyBones.Spine, HumanBodyBones.Chest, 0.12f, 0.13f, 20f),
        new(HumanBodyBones.Chest, HumanBodyBones.Neck, 0.16f, 0.14f, 20f),
        new(HumanBodyBones.Head, HumanBodyBones.LastBone, 0.08f, 0.11f, 40f),
        new(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, 0.03f, 0.045f, 70f),
        new(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, 0.02f, 0.04f, 60f),
        new(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 0.03f, 0.045f, 70f),
        new(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 0.02f, 0.04f, 60f),
        new(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.1f, 0.075f, 50f),
        new(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.06f, 0.055f, 60f),
        new(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.1f, 0.075f, 50f),
        new(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.06f, 0.055f, 60f),
    };

    /// <summary>Превращает врага в рэгдолл. false — не гуманоид, пусть играет анимация смерти.</summary>
    public static bool TryActivate(Animator animator, EnemyHealth health)
    {
        if (animator == null || !animator.isHuman) return false;
        if (animator.GetBoneTransform(HumanBodyBones.Hips) == null) return false;

        var bodies = new Dictionary<Transform, Rigidbody>();
        var colliders = new List<Collider>();

        foreach (BoneSpec spec in Specs)
        {
            Transform bone = animator.GetBoneTransform(spec.Bone);
            if (bone == null) continue;

            Collider collider = bone.GetComponent<Collider>();
            if (collider == null) collider = CreateCollider(animator, bone, spec);
            collider.isTrigger = false;
            collider.enabled = true;
            colliders.Add(collider);

            Rigidbody body = bone.GetComponent<Rigidbody>();
            if (body == null) body = bone.gameObject.AddComponent<Rigidbody>();
            body.mass = TotalMass * spec.MassShare;
            body.isKinematic = false;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            bodies.Add(bone, body);

            Rigidbody parentBody = FindParentBody(bone, bodies);
            if (parentBody != null) AddJoint(body, parentBody, spec.Swing);
        }

        animator.enabled = false;
        IgnorePlayer(colliders);
        PushFromHit(bodies.Values, health);
        return true;
    }

    private static Collider CreateCollider(Animator animator, Transform bone, BoneSpec spec)
    {
        Transform child = spec.Child != HumanBodyBones.LastBone ? animator.GetBoneTransform(spec.Child) : null;

        if (child == null)
        {
            // Голова: сфера чуть выше основания черепа
            SphereCollider sphere = bone.gameObject.AddComponent<SphereCollider>();
            sphere.radius = ToLocal(bone, spec.Radius);
            sphere.center = bone.InverseTransformDirection(Vector3.up) * sphere.radius * 0.8f;
            return sphere;
        }

        Vector3 toChild = bone.InverseTransformPoint(child.position);
        float length = toChild.magnitude;
        CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
        capsule.direction = LargestAxis(toChild);
        capsule.center = toChild * 0.5f;
        capsule.height = length;
        capsule.radius = ToLocal(bone, spec.Radius);
        return capsule;
    }

    // Метры → локальные единицы кости (у Mixamo-скелетов бывает масштаб 0.01)
    private static float ToLocal(Transform bone, float meters)
    {
        float scale = Mathf.Abs(bone.lossyScale.x);
        return scale > 0.0001f ? meters / scale : meters;
    }

    private static int LargestAxis(Vector3 v)
    {
        Vector3 a = new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        if (a.x >= a.y && a.x >= a.z) return 0;
        return a.y >= a.z ? 1 : 2;
    }

    private static Rigidbody FindParentBody(Transform bone, Dictionary<Transform, Rigidbody> bodies)
    {
        for (Transform t = bone.parent; t != null; t = t.parent)
        {
            if (bodies.TryGetValue(t, out Rigidbody body)) return body;
        }
        return null;
    }

    private static void AddJoint(Rigidbody body, Rigidbody parent, float swing)
    {
        CharacterJoint joint = body.gameObject.AddComponent<CharacterJoint>();
        joint.connectedBody = parent;
        joint.enableProjection = true; // не даёт суставам растягиваться при сильных ударах
        joint.lowTwistLimit = new SoftJointLimit { limit = -swing * 0.5f };
        joint.highTwistLimit = new SoftJointLimit { limit = swing * 0.5f };
        joint.swing1Limit = new SoftJointLimit { limit = swing };
        joint.swing2Limit = new SoftJointLimit { limit = swing };
    }

    // Тело не должно толкать игрока и его капсулу
    private static void IgnorePlayer(List<Collider> colliders)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (Collider playerCollider in player.GetComponentsInChildren<Collider>())
        {
            foreach (Collider collider in colliders) Physics.IgnoreCollision(collider, playerCollider);
        }
    }

    private static void PushFromHit(IEnumerable<Rigidbody> bodies, EnemyHealth health)
    {
        if (health == null || health.LastHitDirection == Vector3.zero) return;

        Rigidbody nearest = null;
        float best = float.MaxValue;
        foreach (Rigidbody body in bodies)
        {
            float d = (body.worldCenterOfMass - health.LastHitPoint).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = body;
            }
        }

        if (nearest != null) nearest.AddForceAtPosition(health.LastHitDirection * HitImpulse, health.LastHitPoint, ForceMode.Impulse);
    }
}
