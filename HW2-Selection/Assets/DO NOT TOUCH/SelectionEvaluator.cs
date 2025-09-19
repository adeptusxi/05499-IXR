using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Text = TMPro.TMP_Text;


public class SelectionEvaluator : MonoBehaviour
{
    public GameObject sphere;
    public Transform sphereParent;
    public Transform evaluation;
    public Vector3 spawnBoxCenter;
    public Vector3 spawnBoxDimensions;
    public Vector2 sphereRadiusRange;
    public int sphereCount;
    public int numTrials;
    public float errorPenalty;

    Transform selected;
    Transform target;
    List<Transform> spheres;

    int confirmations;
    int errorCount;
    float lastMadeTarget;
    float trialTime;
    bool inProgress;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartTrial()
    {
        if (spheres != null)
        {
            foreach (Transform t in spheres)
            {
                Destroy(t.gameObject);
            }
        }

        StartCoroutine(StartTrialCoroutine());

    }

    IEnumerator StartTrialCoroutine()
    {

        spheres = new List<Transform>();
        List<Transform> collidingSpheres = new List<Transform>();
        for (int i = 0; i < sphereCount; i++)
        {

            GameObject newSphere = Instantiate(sphere, sphereParent);
            newSphere.SetActive(true);
            spheres.Add(newSphere.transform);
            collidingSpheres.Add(newSphere.transform);
        }
        yield return new WaitForFixedUpdate();

        while (collidingSpheres.Count > 0)
        {

            for (int i = collidingSpheres.Count - 1; i >= 0; i--)
            {
                Collider[] collisions = Physics.OverlapSphere(collidingSpheres[i].position, 2f * collidingSpheres[i].localScale.x);
                if (collisions.Length == 0)
                {
                    collidingSpheres.RemoveAt(i);
                    continue;
                }

                collidingSpheres[i].position = new Vector3(
                    Random.Range(-spawnBoxDimensions.x / 2f, spawnBoxDimensions.x / 2f) + spawnBoxCenter.x,
                    Random.Range(-spawnBoxDimensions.y / 2f, spawnBoxDimensions.y / 2f) + spawnBoxCenter.y,
                    Random.Range(-spawnBoxDimensions.z / 2f, spawnBoxDimensions.z / 2f) + spawnBoxCenter.z
                );
                collidingSpheres[i].localScale = Vector3.one * Random.Range(sphereRadiusRange.x, sphereRadiusRange.y);

            }


        }

        MakeTarget(Random.Range(0, sphereCount));
        evaluation.gameObject.SetActive(false);
        confirmations = 0;
        errorCount = -1;
        trialTime = 0f;
        inProgress = true;
    }

    public void EndTrial()
    {  
        evaluation.gameObject.SetActive(true);
        Text evalText = evaluation.transform.GetComponentInChildren<Text>();
        evalText.text = $"Here are your results:\n\n Average Time: {trialTime} seconds\n Errors: {errorCount+1}\nScore: {Mathf.Max(errorCount,0)*errorPenalty + trialTime}\n\n Press the button below to restart the trial!";
        inProgress = false;
    }

    void MakeTarget(int i)
    {
        if (target)
        {
            target.GetComponent<Renderer>().material.color = Color.white;
        }
        target = spheres[i];
        target.GetComponent<Renderer>().material.color = Color.red;
        lastMadeTarget = Time.time;
    }

    public List<Transform> GetSpheres()
    {
        if (!inProgress)
        {
            Debug.Log("Start trial to get spheres");
            return new List<Transform>();
        }
        return spheres;
    }

    public void SetSelection(Transform newSelected)
    {
        if (newSelected.tag != "Sphere")
        {
            Debug.Log($"Unable to make {newSelected.name} selection");
            return;
        }
        if (!inProgress)
        {
            Debug.Log("Start the trial to begin making selections");
            return;
        }

        Debug.Log($"Selected {newSelected.name}");
        selected = newSelected;
    }

    public void ConfirmSelection()
    {
        if (!inProgress)
        {
            Debug.Log("Start the trial to begin confirming selections");
            return;
        }

        if (selected != target) {
            errorCount++;
        }
        trialTime += (Time.time - lastMadeTarget) / numTrials;
        confirmations++;
        if (confirmations == numTrials)
        {
            EndTrial();
        }
        else
        {
            MakeTarget(Random.Range(0, sphereCount));
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawCube(spawnBoxCenter, spawnBoxDimensions);
    }

}
