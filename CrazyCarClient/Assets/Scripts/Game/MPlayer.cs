﻿using System;
using System.Collections;
using System.Collections.Generic;
using MOBASkill;
using UnityEngine;
using Utils;
using QFramework;
using PathCreation;

public class MPlayer : MonoBehaviour, IController {
    public UserInfo userInfo;

    public Rigidbody rig;

    //输入相关
    public float vInput;
    public float hInput;
    public float sInput;

    public bool isLockSpeed = false;

    //力的大小
    public float currentForce;
    public float normalForce = 20;
    public float boostForce = 40;
    public float jumpForce = 10;

    public float gravity = 20;

    //力的方向
    private Vector3 forceDirHorizontal;

    private float verticalModified; //前后修正系数

    //转弯相关 
    public bool isDrifting;

    public DriftDirection driftDirection = DriftDirection.None;

    //由h_Input以及漂移影响
    public Quaternion rotationStream; //用于最终旋转
    public float turnSpeed = 60;

    Quaternion m_DriftOffset = Quaternion.identity;

    public DriftLevel driftLevel;

    //地面检测
    public LayerMask groundMask = 0;
    public Transform frontHitTrans;
    public Transform rearHitTrans;
    public bool isGround;
    public float groundDistance = 0.7f; //根据车模型自行调节
    public float driftPower = 0;

    public long lastRecvStatusStamp = 0;

    // 出界 翻车判断
    private PathCreator pathCreator;
    private float playerHigh = 2f;
    private Coroutine speedUpCor;
    private float turnoverOffset = 14;
    private float checkTime = 0.4f;
    private float checkTimer = 0;

    // 记录通过拱门的次数
    public int passEndSignTimes = 0;
    private bool isCanCalculatePassEndSignTimes = true;

    // 消除移动时的顿挫感
    private Coroutine moveNetCarCor = null;
    private float smoothSpeed = 4f;
    private Vector3 currentVelocity = Vector3.zero;
    
    ///// 技能系统 /////
    private CharacterSkillSystem skillSystem;
    // MP 玩家的蓝量
    private float mp = 100;
    private float maxMp = 100;

    public float Mp {
        get {
            return mp;
        }
        set {
            mp = value;
            if (mp > maxMp) {
                mp = maxMp;
            } else if (mp < 0) {
                mp = 0;
            }
        }
    }
    ///// 技能系统 ////

    private void Start() {
        pathCreator = this.GetModel<IMapControllerModel>().PathCreator;
        forceDirHorizontal = transform.forward;
        rotationStream = rig.rotation;
        skillSystem = GetComponent<CharacterSkillSystem>();

        StopDrift();
    }

    public void ConfirmStatus(PlayerStateMsg playerStateMsg) {
    }

    private void FixedUpdate() {
        if (isLockSpeed) {
            rig.AddForce(Vector3.zero, ForceMode.Force);
            return;
        }

        if (this.GetModel<IGameModel>().CurGameType == GameType.TimeTrial &&
            this.GetSystem<IPlayerManagerSystem>().SelfPlayer != this) {
            // 为AI终止
            return;
        }

        if (this.GetModel<IGameModel>().CurGameType == GameType.Match &&
            this.GetSystem<IPlayerManagerSystem>().SelfPlayer != this && lastRecvStatusStamp != 0) {
            if (Util.GetTime() - lastRecvStatusStamp > this.GetModel<IGameModel>().MaxSyncDelay) {
                this.GetSystem<IPlayerManagerSystem>().RemovePlayer(userInfo.uid);
            }
        }

        CheckGroundNormal();
        Turn();

        //起步时力大小递增
        IncreaseForce();
        //漂移加速后/松开加油键力大小时递减
        ReduceForce();
        if (isDrifting) {
            CalculateDriftingLevel();
            //ChangeDriftColor();
        }

        //根据上述情况，进行最终的旋转和加力
        rig.MoveRotation(rotationStream);
        CalculateForceDir();
        AddForceToMove();

        checkTimer += Time.fixedDeltaTime;
        if (checkTimer > checkTime) {
            checkTimer = 0;
            if (IsOutside) {
                Debug.Log("++++++ IsOutside ");
                ResetSelfCar();
            }

            if (IsTurnover) {
                Debug.Log("++++++ IsTurnover ");
                ResetSelfCar();
            }
        }
    }

    public void AdjustPosition(Vector3 pos, Vector3 speed) {
        if (this == null) {
            return;
        }

        if (isLockSpeed) {
            return;
        }

        if (moveNetCarCor != null) {
            StopCoroutine(moveNetCarCor);
        }

        moveNetCarCor = StartCoroutine(MoveNetCar(pos, speed));
        lastRecvStatusStamp = Util.GetTime();
    }

    private IEnumerator MoveNetCar(Vector3 pos, Vector3 speed) {
        float time = this.GetModel<IGameModel>().SendMsgOffTime / 2;
        while (time > 0) {
            yield return new WaitForSecondsRealtime(Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * smoothSpeed);
            rig.velocity = Vector3.Lerp(rig.velocity, speed, Time.deltaTime * smoothSpeed);
            rig.velocity = Vector3.SmoothDamp(rig.velocity, speed, ref currentVelocity, Time.deltaTime * smoothSpeed);
            time -= Time.deltaTime;
        }
    }

    //计算加力方向
    private void CalculateForceDir() {
        //往前加力
        if (vInput > 0) {
            verticalModified = 1;
        } else if (vInput < 0) {
            verticalModified = -1;
        }

        forceDirHorizontal = m_DriftOffset * transform.forward;
    }

    //加力移动
    private void AddForceToMove() {
        //计算合力
        Vector3 tempForce = verticalModified * currentForce * forceDirHorizontal;

        if (this.GetSystem<IPlayerManagerSystem>().SelfPlayer == this && !isGround) {
            tempForce = tempForce + gravity * Vector3.down;
        }

        rig.AddForce(tempForce, ForceMode.Force);
    }

    private bool IsOutside {
        get {
            if (pathCreator != null) {
                Vector3 closestPos = pathCreator.path.GetClosestPointOnPath(transform.position);
                float dis = Vector3.Distance(closestPos, transform.position);
                return dis > this.GetModel<IMapControllerModel>().RoadWidth;
            } else {
                return false;
            }
        }
    }

    private bool IsTurnover {
        get {
            float time = pathCreator.path.GetClosestTimeOnPath(transform.position);
            Vector3 pathNor = pathCreator.path.GetNormal(time);
            return Mathf.Abs(Util.GetAngle(pathNor, transform.up)) > (turnoverOffset + Util.pathRotateOffset.magnitude);
        }
    }

    private void ResetPeerCar() {
        transform.position = pathCreator.path.GetClosestPointOnPath(transform.position) + new Vector3(0, playerHigh, 0);
        float distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        transform.rotation = Quaternion.Euler(
            pathCreator.path.GetRotationAtDistance(distanceTravelled, EndOfPathInstruction.Loop).eulerAngles +
            Util.pathRotateOffset);
    }

    private void ResetSelfCar() {
        StopSpeedUp();
        currentForce = 0;
        rig.velocity = Vector3.zero;
        transform.position = pathCreator.path.GetClosestPointOnPath(transform.position) + new Vector3(0, playerHigh, 0);
        float distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        rotationStream = Quaternion.Euler(
            pathCreator.path.GetRotationAtDistance(distanceTravelled, EndOfPathInstruction.Loop).eulerAngles +
            Util.pathRotateOffset);
    }


    //检测是否在地面上，并且使车与地面保持水平
    private void CheckGroundNormal() {
        //从车头中心附近往下打射线,长度比发射点到车底的距离长一点
        RaycastHit frontHit;
        bool hasFrontHit =
            Physics.Raycast(frontHitTrans.position, -transform.up, out frontHit, groundDistance, groundMask);
        if (hasFrontHit) {
            Debug.DrawLine(frontHitTrans.position, frontHitTrans.position - transform.up * groundDistance, Color.red);
        }

        RaycastHit rearHit;
        bool hasRearHit =
            Physics.Raycast(rearHitTrans.position, -transform.up, out rearHit, groundDistance, groundMask);
        if (hasRearHit) {
            Debug.DrawLine(rearHitTrans.position, rearHitTrans.position - transform.up * groundDistance, Color.red);
        }

        if (hasFrontHit || hasRearHit) {
            isGround = true;
        } else {
            isGround = false;
        }

        //使车与地面水平
        Vector3 tempNormal = (frontHit.normal + rearHit.normal).normalized;
        Quaternion quaternion = Quaternion.FromToRotation(transform.up, tempNormal);
        rotationStream = quaternion * rotationStream;
    }

    //力递减
    private void ReduceForce() {
        float targetForce = currentForce;
        if (isGround && vInput == 0) {
            targetForce = 0;
        } else if (currentForce > normalForce) {
            targetForce = normalForce;
        }

        currentForce = Mathf.MoveTowards(currentForce, targetForce, 30 * Time.fixedDeltaTime); //每秒60递减，可调
    }

    //力递增
    private void IncreaseForce() {
        float targetForce = currentForce;
        if (vInput != 0 && currentForce < normalForce) {
            currentForce = Mathf.MoveTowards(currentForce, normalForce, 80 * Time.fixedDeltaTime); //每秒80递增
        }
    }

    private void Turn() {
        if (rig.velocity.sqrMagnitude <= 0.1) {
            return;
        }

        //漂移时自带转向
        if (driftDirection == DriftDirection.Left) {
            rotationStream = rotationStream * Quaternion.Euler(0, -40 * Time.fixedDeltaTime, 0);
        } else if (driftDirection == DriftDirection.Right) {
            rotationStream = rotationStream * Quaternion.Euler(0, 40 * Time.fixedDeltaTime, 0);
        }

        //后退时左右颠倒
        float modifiedSteering = Vector3.Dot(rig.velocity, transform.forward) >= 0 ? hInput : -hInput;

        //输入可控转向：如果在漂移，可控角速度为30，否则平常状态为60.
        turnSpeed = driftDirection != DriftDirection.None ? 30 : 60;
        float turnAngle = modifiedSteering * turnSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0, turnAngle, 0);
        //局部坐标下旋转,这里有空换一个简单的写法
        rotationStream = rotationStream * deltaRotation;
    }

    private void Jump() {
        rig.AddForce(jumpForce * transform.up, ForceMode.Impulse);
    }


    //开始漂移并且决定漂移朝向
    public void StartDrift() {
        //Debug.Log("Start Drift");
        isDrifting = true;

        //根据水平输入决定漂移时车的朝向，因为合速度方向与车身方向不一致，所以为加力方向添加偏移
        if (hInput < 0) {
            driftDirection = DriftDirection.Left;
            //左漂移时，合速度方向为车头朝向的右前方，偏移具体数值需结合实际自己调试
            m_DriftOffset = Quaternion.Euler(0f, 30, 0f);
        } else if (hInput > 0) {
            driftDirection = DriftDirection.Right;
            m_DriftOffset = Quaternion.Euler(0f, -30, 0f);
        }
    }

    //计算漂移等级
    private void CalculateDriftingLevel() {
        driftPower += Time.fixedDeltaTime;
        //Debug.LogError("+++++++driftPower = " + driftPower + "  driftLevel = " + driftLevel);
        //0.7秒提升一个漂移等级
        if (driftPower < 0.7) {
            driftLevel = DriftLevel.One;
        } else if (driftPower < 1.4) {
            driftLevel = DriftLevel.Two;
        } else {
            driftLevel = DriftLevel.Three;
        }
    }

    public void StopDrift() {
        isDrifting = false;
        driftDirection = DriftDirection.None;
        driftPower = 0;
        m_DriftOffset = Quaternion.identity;
    }

    public void Boost(float boostForce) {
        //按照漂移等级加速：1 / 1.1 / 1.2
        currentForce = (1 + (int)driftLevel / 10) * boostForce;
    }

    public void SpeedUp(float time) {
        speedUpCor = StartCoroutine(KeepSpeedUp(time));
    }

    private IEnumerator KeepSpeedUp(float time) {
        float cF = currentForce * 1.6f;
        while (time > 0) {
            currentForce = cF;
            time -= Time.fixedDeltaTime;
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
    }

    private void StopSpeedUp() {
        if (speedUpCor != null) {
            StopCoroutine(speedUpCor);
        }
    }

    public void UpdateParameter() {
        normalForce = userInfo.equipInfo.power;
        boostForce = userInfo.equipInfo.maxPower;
        gravity = userInfo.equipInfo.mass;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == TagName.endSign) {
            if (isCanCalculatePassEndSignTimes) {
                isCanCalculatePassEndSignTimes = false;
                passEndSignTimes++;
                Util.DelayExecuteWithSecond(3, () => { isCanCalculatePassEndSignTimes = true; });
            }
        }
    }

    public void DestroySelf() {
        Destroy(gameObject);
    }

    public void BeHit(float slowAmount, bool isCrit) {
        // 减少当前的速度
        currentForce -= slowAmount;

        // 确保速度不会低于0
        if (currentForce < 0) {
            currentForce = 0;
        }
    }

    public void Attack(int id) {
        skillSystem.OnSkillChoose(id);
    }

    public IArchitecture GetArchitecture() {
        return CrazyCar.Interface;
    }
}