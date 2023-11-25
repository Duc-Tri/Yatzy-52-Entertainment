using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

using Random = UnityEngine.Random;

public class RollDice : MonoBehaviour
{
    [SerializeField] private bool cheatAlways6 = false;

    [SerializeField] private GameObject dice; // 3D dice on scene
    private Transform diceTrans;
    private Button buttonRollDice;

    private int diceResult; // randomized at button press, before animation

    [SerializeField] private TextMeshProUGUI scoreTMP;
    private int score;

    // Rolling parameters -------------------------------------------------------------------------
    [SerializeField] private float rollingTime = 1; // in seconds
    [SerializeField] private float initialSpeedRot = 1000;
    [SerializeField] private float finalSpeedRot = 10;
    private float speedRot; // diminuing speed rotation
    private float startRollingTime; // to measure duration of rolling from
    private Vector3 rollingDir1, rollingDir2; // rolling directions
    private Vector3 currentAngle, finalAngle; // current angle & final angle the dice MUST have

    // Ending parameters --------------------------------------------------------------------------
    [SerializeField] private float speedLerpAngle = 20;
    private int framesCount; // secure the halt of dice in ending phase

    private bool updateRolling; // update rolling phase ?
    private bool updateEnding; // update ending phase ?

    // Euler angles of dice for to each face ------------------------------------------------------
    static readonly Vector3[] numberAngles = {
        new Vector3( 90, 0, 0 ), // 1
        new Vector3( 0, 270, 0 ), // 2
        new Vector3( 0, 180, 0 ), // 3
        new Vector3( 0, 0, 0 ), // 4
        new Vector3( 0, 90, 0 ), // 5
        new Vector3( 270, 0, 0 ), // 6
    };

    // For yield and tweens -----------------------------------------------------------------------
    private const float TWEEN_SHORT_TIME = 0.1f;
    private const float TWEEN_MEDIUM_TIME = 0.5f;
    private readonly WaitForEndOfFrame waitFrame = new WaitForEndOfFrame();
    private readonly WaitForSeconds waitShortTime = new WaitForSeconds(TWEEN_SHORT_TIME);
    private readonly WaitForSeconds waitMoreTime = new WaitForSeconds(TWEEN_MEDIUM_TIME);

    private bool NearlyEquals(float a, float b, float tolerance = 0.01f) => Math.Abs(a - b) <= tolerance;
    private bool IsNearly0Or360(float a) => NearlyEquals(a, 360) || NearlyEquals(a, 0);

    private void Awake()
    {
        InitGame();
    }

    void Update()
    {
        if (updateRolling)
        {
            RollingDice();
            if (Time.time - startRollingTime >= rollingTime)
                PrepareEnding();
        }
        else if (updateEnding)
        {
            EndRolling();
        }
    }

    private void InitGame()
    {
        updateEnding = updateRolling = false;
        diceTrans = dice.transform;
        diceTrans.rotation = Quaternion.identity;
        score = 0;
        AddAndUpdateScore(0);
    }

    // Used by scene button
    private void OnClickRollDice(Button b)
    {
        if (buttonRollDice != b)
            buttonRollDice = b;

        StartCoroutine(StartRolling());
    }

    // Starting rolling dice, the result is known before rolling animation
    private IEnumerator StartRolling()
    {
        buttonRollDice.interactable = false; // important
        buttonRollDice.transform.DOScale(Vector3.one * 0.75f, TWEEN_SHORT_TIME).SetEase(Ease.InOutElastic);

        diceResult = cheatAlways6 ? 6 : Random.Range(1, 7); // important
        finalAngle = numberAngles[diceResult - 1];

        speedRot = initialSpeedRot;
        diceTrans.rotation = Quaternion.identity;

        startRollingTime = Time.time; // important

        // to add some random in rotations directions
        rollingDir1 = (Random.value > 0.5f ? Vector3.up : Vector3.down);
        rollingDir2 = (Random.value > 0.5f ? Vector3.right : Vector3.left);

        updateEnding = false;
        updateRolling = true;

        yield return waitShortTime;

        buttonRollDice.transform.DOScale(Vector3.one, TWEEN_SHORT_TIME).SetEase(Ease.InOutElastic);
    }

    // Make rotation of the dice, in 2 random directions, slowing down with time
    private void RollingDice()
    {
        float speed = speedRot * Time.deltaTime;
        diceTrans.RotateAround(diceTrans.position, rollingDir1, speed);
        diceTrans.RotateAround(diceTrans.position, rollingDir2, speed);

        speedRot = Mathf.Lerp(speedRot, finalSpeedRot, 1.25f * Time.deltaTime);
    }

    private void PrepareEnding()
    {
        framesCount = 0;
        currentAngle = diceTrans.eulerAngles;

        updateRolling = false;
        updateEnding = true;
    }

    private void EndRolling()
    {
        float speed = speedLerpAngle * Time.deltaTime;
        currentAngle = new Vector3(Mathf.LerpAngle(currentAngle.x, finalAngle.x, speed),
                                   Mathf.LerpAngle(currentAngle.y, finalAngle.y, speed),
                                   Mathf.LerpAngle(currentAngle.z, finalAngle.z, speed));

        diceTrans.eulerAngles = currentAngle;

        // differences between current & target angle
        float diffAngleX = MathF.Abs(currentAngle.x - finalAngle.x);
        float diffAngleY = MathF.Abs(currentAngle.y - finalAngle.y);
        float diffAngleZ = MathF.Abs(currentAngle.z - finalAngle.z);

        // are we finished ?
        if (++framesCount > 99 || IsNearly0Or360(diffAngleX) && IsNearly0Or360(diffAngleY) && IsNearly0Or360(diffAngleZ))
        {
            updateEnding = false;
            diceTrans.eulerAngles = finalAngle;

            if (diceResult == 6)
                StartCoroutine(DiceFaceIs6());
            else
                buttonRollDice.interactable = true;
        }
    }

    // Make some animations with tweens, update the score
    private IEnumerator DiceFaceIs6()
    {
        diceTrans.DOShakePosition(2f * TWEEN_MEDIUM_TIME, 0.05f);
        diceTrans.DOScale(Vector3.one * 1.75f, TWEEN_MEDIUM_TIME).SetEase(Ease.InOutElastic);
        scoreTMP.transform.DOScale(Vector3.one * 2f, 0.125f);

        yield return waitFrame;

        AddAndUpdateScore(1); // important

        yield return waitMoreTime;

        diceTrans.DOScale(Vector3.one, TWEEN_MEDIUM_TIME).SetEase(Ease.InOutElastic);
        scoreTMP.transform.DOScale(Vector3.one, 0.1f);

        yield return waitMoreTime;

        diceTrans.eulerAngles = finalAngle;
        buttonRollDice.interactable = true; // important
    }

    private void AddAndUpdateScore(int addValue = 0)
    {
        score += addValue;
        scoreTMP.text = score.ToString();
    }

}
