using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class EnemyChaser : MonoBehaviour
{
	[Header("Comportamiento")]
    public Transform target;
    public float moveSpeed = 3.8f;
	[Min(1f)] public float detectionRange = 10f;
	[Min(1f)] public float patrolRadius = 7f;
	[Min(2)] public int patrolPointCount = 4;
	[Min(0.1f)] public float waypointReachDistance = 0.65f;
    public float catchDistance = 1.05f;
    bool caught;
    Terrain currentTerrain;
	Vector3[] patrolPoints;
	int patrolIndex;
	Vector3 homePosition;

	public enum BehaviorState { Patrol, Chase }
	public BehaviorState CurrentState { get; private set; }

    void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

		homePosition = transform.position;
		BuildPatrolRoute();
		// Direct movement keeps this enemy functional without requiring a baked NavMesh.
        SnapToGround();
    }

    void Update()
    {
        if (caught || target == null) return;
		Vector3 flatOffset = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
		if (flatOffset.sqrMagnitude <= catchDistance * catchDistance &&
            Mathf.Abs(target.position.y - transform.position.y) < 2f) { CatchPlayer(); return; }

		CurrentState = flatOffset.sqrMagnitude <= detectionRange * detectionRange
			? BehaviorState.Chase
			: BehaviorState.Patrol;

		Vector3 destination = CurrentState == BehaviorState.Chase
			? target.position
			: GetPatrolDestination();
		MoveTowards(destination);
        SnapToGround();
    }

	void BuildPatrolRoute()
	{
		int count = Mathf.Max(2, patrolPointCount);
		patrolPoints = new Vector3[count];
		for (int i = 0; i < count; i++)
		{
			float angle = (Mathf.PI * 2f * i) / count;
			Vector3 point = homePosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * patrolRadius;
			patrolPoints[i] = GetGroundPosition(point);
		}
		patrolIndex = 0;
	}

	Vector3 GetPatrolDestination()
	{
		if (patrolPoints == null || patrolPoints.Length == 0) return homePosition;
		Vector3 offset = Vector3.ProjectOnPlane(patrolPoints[patrolIndex] - transform.position, Vector3.up);
		if (offset.sqrMagnitude <= waypointReachDistance * waypointReachDistance)
			patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
		return patrolPoints[patrolIndex];
	}

	void MoveTowards(Vector3 destination)
	{
		Vector3 offset = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
		if (offset.sqrMagnitude <= 0.01f) return;
		Vector3 direction = offset.normalized;
		transform.position += direction * moveSpeed * Time.deltaTime;
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
	}

	Vector3 GetGroundPosition(Vector3 point)
	{
		foreach (Terrain terrain in Terrain.activeTerrains)
		{
			if (!Contains(terrain, point)) continue;
			point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
			break;
		}
		return point;
	}

    void SnapToGround()
    {
        Vector3 position = transform.position;
        if (currentTerrain == null || !Contains(currentTerrain, position))
        {
            currentTerrain = null;
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                if (Contains(terrain, position)) { currentTerrain = terrain; break; }
            }
        }
        if (currentTerrain == null) return;
        position.y = currentTerrain.SampleHeight(position) + currentTerrain.transform.position.y;
        transform.position = position;
    }

    static bool Contains(Terrain terrain, Vector3 position)
    {
        if (terrain == null || terrain.terrainData == null) return false;
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return position.x >= origin.x && position.x <= origin.x + size.x &&
               position.z >= origin.z && position.z <= origin.z + size.z;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) CatchPlayer();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player")) CatchPlayer();
    }

    void CatchPlayer()
    {
        if (caught) return;
        caught = true;
        PA3MenuController.ReturnToMainMenu("El guardián te atrapó. Tu partida se reinició: inténtalo otra vez.");
    }
}
