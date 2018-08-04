using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;

public class MyMovementAI : MonoBehaviour {

    private Blackboard sharedBlackboard;
    private Blackboard ownBlackboard;

    private Root behaviorTree;

    private string[] visited;
    private string currentDest; 

    private bool reached = false;

    private UnityEngine.AI.NavMeshAgent navAgent;

    private bool myRoutineStarted = false;

    [SerializeField] private GameObject[] areaDest;

    void Start()
    {
        visited = new string[areaDest.Length];

        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        sharedBlackboard = UnityContext.GetSharedBlackboard("SharedData");
        ownBlackboard = new Blackboard(sharedBlackboard, UnityContext.GetClock());
        
        behaviorTree = CreateBehaviourTree();

        
#if UNITY_EDITOR
        Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
        debugger.BehaviorTree = behaviorTree;
#endif

        behaviorTree.Start();
    }

    private Root CreateBehaviourTree()
    {
        return new Root(ownBlackboard,

            new Service(1f, UpdateBlackboards,

                new Selector(

                    new BlackboardCondition("Reached", Operator.IS_EQUAL, false, Stops.IMMEDIATE_RESTART,
                        
                        new Sequence(

                            new Action(() => SetDestination()) { Label = "Setting Destination" },
                            new Action(() => RemainingDistanceCheck()) { Label = "Checking Remaining Distance" }


                        )
                    ),
                    
                    new Selector(

                        new BlackboardCondition("Reached", Operator.IS_EQUAL, true, Stops.IMMEDIATE_RESTART,

                            new Sequence(
                                new Action(() => NextDestWaitTime()) { Label = "Wait for random time" }
                                )
                            )
                        )
                    )
                ));
    }

    private void UpdateBlackboards()
    {
        ownBlackboard["Reached"] = reached;
    }


    private void SetDestination()
    {
        for (int i = 0; i < areaDest.Length; i++)
        {
            for (int j = 0; j < areaDest[i].transform.childCount; j++)
            {
                GameObject tempChild = areaDest[i].transform.GetChild(j).gameObject;
                string sharedBlackboardName = tempChild.name + tempChild.transform.parent.name;

                if (sharedBlackboard.Get<string>(sharedBlackboardName) != "Occupied" &&
                    ownBlackboard.Get<bool>("DestinationSet") != true && visited[i] != tempChild.transform.parent.name)
                {
                    reached = false;
                    navAgent.SetDestination(tempChild.transform.position);
                    visited[i] = tempChild.transform.parent.name;
                    currentDest = sharedBlackboardName;

                    sharedBlackboard[sharedBlackboardName] = "Occupied";
                    ownBlackboard["DestinationSet"] = true;

                    Debug.Log("destination set");
                    return;
                }
            }
        }
    }


    private void RemainingDistanceCheck()
    {
        Vector3 tempDest = navAgent.destination;
        float distance = (transform.position - tempDest).magnitude;

        if (distance <= 2 && ownBlackboard.Get<bool>("Reached") != true)
        {
            reached = true;

            //ownBlackboard["Reached"] = true;

            Debug.Log("destination reached");
        }
    }

    private void NextDestWaitTime()
    {
        
        if (myRoutineStarted == false)
        {
            Debug.Log("waiting");
            StartCoroutine(waiter());
            myRoutineStarted = true;
        }
        //StartCoroutine(waiter());

    }

    private void ClearUsedData()
    {
        ownBlackboard["DestinationSet"] = false;
        sharedBlackboard[currentDest] = "NotOccupied";

        reached = false;
        myRoutineStarted = false;

        if (currentDest.Contains("Final"))
        {
            Destroy(this.gameObject);
        }
    }

    IEnumerator waiter()
    {
        float waitTime = UnityEngine.Random.Range(5f, 15f);
        yield return new WaitForSeconds(waitTime);
        print("Waited for " + waitTime);
        ClearUsedData();
    }
}

