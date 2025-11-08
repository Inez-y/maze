// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;

// public class EnemyController : MonoBehaviour
// {
//     [Header("Refs")]
//     public MazeGenerator maze;    
//     public Animator animator;     
    
//     [Header("Movement")]
//     public float moveSpeed = 2.2f; // units/sec
//     public float waitAtCell = 0.4f; // pause before picking next cell
//     public float yOffset = 0.0f; // raise model if it sinks into floor
    
//     // internal state
//     int cx, cy; // current cell
//     int prevCx, prevCy; // to avoid immediate backtracking (optional)
//     Vector3 targetPos;

//     void Awake()
//     {
//         if (!animator) animator = GetComponentInChildren<Animator>();
//     }

//     IEnumerator Start()
//     {
//         var (sx, sy) = (maze.width - 1, maze.height - 1);
//         (cx, cy) = (sx, sy);
//         transform.position = maze.CellCenter(cx, cy) + Vector3.up * yOffset;

//         // start moving forever
//         while (true)
//         {
//             yield return PickNextCellAndWalk();
//         }
//     }

//     IEnumerator PickNextCellAndWalk()
//     {
//         // get open neighbors, try to avoid immediately going back to previous cell
//         var neighbors = new List<(int,int)>(maze.OpenNeighbors(cx, cy).Select(n => (n.nx, n.ny)));
//         if (neighbors.Count == 0)
//         {
//             // dead end: just wait and try again 
//             yield return new WaitForSeconds(waitAtCell);
//             yield break;
//         }

//         // remove the previous cell from choices if there are alternatives
//         if (neighbors.Count > 1)
//             neighbors.RemoveAll(n => n.Item1 == prevCx && n.Item2 == prevCy);

//         // pick random neighbor
//         var next = neighbors[Random.Range(0, neighbors.Count)];
//         prevCx = cx; prevCy = cy;
//         cx = next.Item1; cy = next.Item2;
//         targetPos = maze.CellCenter(cx, cy) + Vector3.up * yOffset;

//         // walk toward target
//         // set animation parameter if you have one
//         if (animator) animator.SetFloat("Speed", moveSpeed); // or SetBool("IsMoving", true)

//         // move smoothly
//         while ((transform.position - targetPos).sqrMagnitude > 0.001f)
//         {
//             transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

//             // face movement direction (optional)
//             Vector3 dir = (targetPos - transform.position); dir.y = 0f;
//             if (dir.sqrMagnitude > 0.0001f)
//                 transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

//             yield return null;
//         }

//         // arrived at cell center
//         if (animator) animator.SetFloat("Speed", 0f); 
//         yield return new WaitForSeconds(waitAtCell);
//     }
// }
