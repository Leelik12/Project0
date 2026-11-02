using UnityEngine;

/// <summary>
/// Реакция на попадание без отдельной анимации: корпус и голова дёргаются от пули поверх текущей анимации
/// (дополнительный поворот костей в LateUpdate, после Animator) и плавно возвращаются.
/// Добавляется из EnemyHealth гуманоидам.
/// </summary>
public class EnemyHitReaction : MonoBehaviour
{
    private const float BodyKick = 14f;     // градусов на попадание в корпус
    private const float HeadKick = 28f;     // градусов на хедшот
    private const float MaxAngle = 35f;
    private const float RecoverSpeed = 9f;

    private Transform spine;
    private Transform chest;
    private Transform head;

    private float bodyAngle;
    private float headAngle;
    private Vector3 axis = Vector3.right; // ось наклона в мировых координатах

    public void Init(Animator animator)
    {
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
    }

    /// <summary>Толчок от удара, прилетевшего в направлении direction.</summary>
    public void Flinch(Vector3 direction, bool headshot)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = -transform.forward; // направление неизвестно — «в грудь»
        axis = Vector3.Cross(Vector3.up, direction.normalized); // наклон по направлению полёта пули

        bodyAngle = Mathf.Min(bodyAngle + BodyKick, MaxAngle);
        if (headshot) headAngle = Mathf.Min(headAngle + HeadKick, MaxAngle);
    }

    private void LateUpdate()
    {
        if (bodyAngle < 0.01f && headAngle < 0.01f) return;

        float k = 1f - Mathf.Exp(-RecoverSpeed * Time.deltaTime);
        bodyAngle = Mathf.Lerp(bodyAngle, 0f, k);
        headAngle = Mathf.Lerp(headAngle, 0f, k);

        // Наклон распределён по позвоночнику, чтобы изгиб выглядел естественно
        Rotate(spine, bodyAngle * 0.4f);
        Rotate(chest, bodyAngle * 0.6f);
        Rotate(head, headAngle);
    }

    private void Rotate(Transform bone, float degrees)
    {
        if (bone != null) bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
    }
}
