﻿using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    const int NUMBER_OF_LANES = 3;
    const int LANE_LENGTH = 12;

    const float LANE_CHANGE_TIME = 0.05f;
    const float PERMANENT_SPEED_GAIN_TIME = 60f;
    const float OBSTACLE_LOST_SPEED_GAIN_TIME = 3f;
    const float DISH_SPEED_GAIN_TIME = 5f;
    const float BOOST_DEACCEL_TIME = 0.5f;

    const float INITIAL_SPEED = 10f;
    const float PERMANENT_SPEED_GAIN = 1f;
    const float OBSTACLE_SPEED_GAIN = 1f;
    public const float INGREDIENT_SPEED_GAIN = 1f;

    const float EPS = 0.01f;

    // Input Variables
    public InputAction jumpAction;
    public InputAction moveLeftAction;
    public InputAction moveRightAction;

    // Player Variables
    private Rigidbody _body;
    private float jumpHeight = 2f;
    [SerializeField]
    private float maxSpeed;
    [SerializeField]
    private float currentSpeed;
    public float gameOverSpeed = 2f;
    private int currentLane = 2;
    private float obstacleSpeedGainRemainder = 0f;
    private float dishSpeedGainRemainder = 0f;
    private bool inMovement = false;
    private bool gameOverState = false;
    private float starting_elevation;
    private Vector3 shift;

    // Inventory Variables
    [SerializeField]
    private GameObject inventory;
    private PlayerInventoryData playerInventoryData;

    // Timers
    float movementTimeCount;
    float permanentSpeedCount;
    float obstacleSpeedCount;
    float ingredientSpeedCount;

    // Obstacle Collisions
    private float speedReduction = 0f;
    private bool isInvincible = false;
    [SerializeField]
    private float invincibilityDuration = 1f;

    void Start()
    {
        maxSpeed = INITIAL_SPEED;
        currentSpeed = INITIAL_SPEED;

        jumpAction.performed += ctx => jump();
        moveLeftAction.performed += ctx => moveLeft();
        moveRightAction.performed += ctx => moveRight();

        _body = gameObject.GetComponent<Rigidbody>();
        starting_elevation = _body.transform.position.y;

        inventory = GameObject.Find("Inventory");
        playerInventoryData = inventory.GetComponent<PlayerInventoryData>();
    }

    void Update()
    {
        if (canPlayerMove())
        {
            jumpAction.Enable();
            moveLeftAction.Enable();
            moveRightAction.Enable();
        }
        else
        {
            jumpAction.Disable();
            moveLeftAction.Disable();
            moveRightAction.Disable();
        }

        updateSpeed();
        moveBody();
        checkGameOver();
    }

    private void updateSpeed()
    {
        if (speedReduction != 0f)
        {
            currentSpeed -= speedReduction;
            speedReduction = 0f;
        }

        gainPermanentSpeed();
        gainLostSpeedFromObstacle();
        boostSpeedFromCreatingDish();
    }

    private void moveBody()
    {
        _body.MovePosition(_body.position + (Time.deltaTime * new Vector3(0, 0, currentSpeed)));
        if (inMovement)
        {
            _body.MovePosition(_body.position + (Mathf.Min(LANE_CHANGE_TIME - movementTimeCount, Time.deltaTime) * shift / LANE_CHANGE_TIME));
            movementTimeCount += Time.deltaTime;
            if (movementTimeCount >= LANE_CHANGE_TIME)
            {
                inMovement = false;
                shift = Vector3.zero;
                movementTimeCount = 0;
            }
        }
    }

    private void checkGameOver()
    {
        if (currentSpeed <= gameOverSpeed)
        {
            gameOverState = true;
            enabled = false;
        }
    }

    private void jump()
    {
        if (_body != null)
        {
            _body.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
        }
    }

    private void moveLeft()
    {
        if (currentLane > 1)
        {
            inMovement = true;
            shift = new Vector3(-LANE_LENGTH / (NUMBER_OF_LANES + 1), 0, 0);
            currentLane--;
        }
    }

    private void moveRight()
    {
        if (currentLane < NUMBER_OF_LANES)
        {
            inMovement = true;
            shift = new Vector3(LANE_LENGTH / (NUMBER_OF_LANES + 1), 0, 0);
            currentLane++;
        }
    }

    private void gainLostSpeedFromObstacle()
    {
        if (obstacleSpeedGainRemainder > 0)
        {
            obstacleSpeedCount += Time.deltaTime;
            if (obstacleSpeedCount >= OBSTACLE_LOST_SPEED_GAIN_TIME)
            {
                if (obstacleSpeedGainRemainder < OBSTACLE_SPEED_GAIN)
                {
                    currentSpeed += obstacleSpeedGainRemainder;
                    obstacleSpeedGainRemainder -= obstacleSpeedGainRemainder;
                }
                else
                {
                    currentSpeed += OBSTACLE_SPEED_GAIN;
                    obstacleSpeedGainRemainder -= OBSTACLE_SPEED_GAIN;
                }
                obstacleSpeedCount = 0;
            }
        }
    }

    private IEnumerator boostSpeed(float speedGain)
    {        
        isInvincible = true;
        Debug.Log("starting speed: " + currentSpeed);
        currentSpeed += speedGain;
        Debug.Log("start boost: " + currentSpeed);
        for (float i = 0; i < DISH_SPEED_GAIN_TIME; i += Time.deltaTime)
        {
            // TODO: Can add visual cues for invincibility  
            yield return new WaitForSeconds(Time.deltaTime);
        }
        Debug.Log("done increase boost");
        float remaining = speedGain;
        for (float i = 0; i < BOOST_DEACCEL_TIME; i += Time.deltaTime)
        {
            float deaccel = Time.deltaTime * speedGain / BOOST_DEACCEL_TIME;
            currentSpeed -= deaccel;
            remaining -= deaccel;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        currentSpeed -= remaining;
        isInvincible = false;
        Debug.Log("end boost: " + currentSpeed);
    }

    private void boostSpeedFromCreatingDish()
    {
        if (dishSpeedGainRemainder > 0) {
            StartCoroutine(boostSpeed(dishSpeedGainRemainder));
            dishSpeedGainRemainder = 0;
        }
    }

    private void gainPermanentSpeed()
    {
        permanentSpeedCount += Time.deltaTime;
        if (permanentSpeedCount >= PERMANENT_SPEED_GAIN_TIME)
        {
            maxSpeed += PERMANENT_SPEED_GAIN;
            currentSpeed += PERMANENT_SPEED_GAIN;
            permanentSpeedCount = 0;
        }
    }

    private IEnumerator becomeInvincibleTemporary()
    {
        isInvincible = true;
        for (float i = 0; i < invincibilityDuration; i += Time.deltaTime)
        {
            // TODO: Can add visual cues for invincibility  
            yield return new WaitForSeconds(Time.deltaTime);
        }
        isInvincible = false;
    }

    private bool canPlayerMove()
    {
        return inMovement == false && (_body.position.y <= starting_elevation + EPS) && (starting_elevation - EPS <= _body.position.y);
    }

    public bool GetGameOverState()
    {
        return gameOverState;
    }

    public void SlowDown(float reduction, bool isObstacle = true)
    {
        if (isObstacle)
        {
            if (!isInvincible)
            {
                speedReduction = (maxSpeed / reduction);
                obstacleSpeedGainRemainder += (maxSpeed / reduction);
                StartCoroutine(becomeInvincibleTemporary());
            }
        }
        else
        {   
            speedReduction = reduction;
        }
    }

    public void AddToInventory(string ingredient, Sprite inventoryImage)
    {
        int usedIngredientCount = playerInventoryData.AddIngredient(ingredient, inventoryImage);
        if (usedIngredientCount > 0) {
            dishSpeedGainRemainder += usedIngredientCount * INGREDIENT_SPEED_GAIN;
        }
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public Dictionary<string, int> GetCollectedIngredientsCounts()
    {
        return playerInventoryData.GetCollectedIngredientsCounts();
    }

    public List<RecipeController> GetRecipes()
    {
        return playerInventoryData.GetRecipes();
    }
}

