using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Event;

/// <summary>
/// 战斗业务事件定义集合，用于在热更新示例中解耦分数、死亡、爆炸和射击等战斗状态通知。
/// </summary>
public class BattleEventDefine
{
    /// <summary>
    /// 分数改变
    /// </summary>
    public class ScoreChange : IEventMessage
    {
        public int CurrentScores;

        /// <summary>
        /// 发送分数变化事件。
        /// </summary>
        public static void SendEventMessage(int currentScores)
        {
            var msg = new ScoreChange();
            msg.CurrentScores = currentScores;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public class GameOver : IEventMessage
    {
        /// <summary>
        /// 发送游戏结束事件。
        /// </summary>
        public static void SendEventMessage()
        {
            var msg = new GameOver();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 敌人死亡
    /// </summary>
    public class EnemyDead : IEventMessage
    {
        public Vector3 Position;
        public Quaternion Rotation;

        /// <summary>
        /// 发送敌人死亡事件，并携带死亡位置与旋转。
        /// </summary>
        public static void SendEventMessage(Vector3 position, Quaternion rotation)
        {
            var msg = new EnemyDead();
            msg.Position = position;
            msg.Rotation = rotation;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 玩家死亡
    /// </summary>
    public class PlayerDead : IEventMessage
    {
        public Vector3 Position;
        public Quaternion Rotation;

        /// <summary>
        /// 发送玩家死亡事件，并携带死亡位置与旋转。
        /// </summary>
        public static void SendEventMessage(Vector3 position, Quaternion rotation)
        {
            var msg = new PlayerDead();
            msg.Position = position;
            msg.Rotation = rotation;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 小行星爆炸
    /// </summary>
    public class AsteroidExplosion : IEventMessage
    {
        public Vector3 Position;
        public Quaternion Rotation;

        /// <summary>
        /// 发送小行星爆炸事件，并携带爆炸位置与旋转。
        /// </summary>
        public static void SendEventMessage(Vector3 position, Quaternion rotation)
        {
            var msg = new AsteroidExplosion();
            msg.Position = position;
            msg.Rotation = rotation;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 敌人发射子弹
    /// </summary>
    public class EnemyFireBullet : IEventMessage
    {
        public Vector3 Position;
        public Quaternion Rotation;

        /// <summary>
        /// 发送敌人开火事件，并携带子弹生成位置与旋转。
        /// </summary>
        public static void SendEventMessage(Vector3 position, Quaternion rotation)
        {
            var msg = new EnemyFireBullet();
            msg.Position = position;
            msg.Rotation = rotation;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 玩家发射子弹
    /// </summary>
    public class PlayerFireBullet : IEventMessage
    {
        public Vector3 Position;
        public Quaternion Rotation;

        /// <summary>
        /// 发送玩家开火事件，并携带子弹生成位置与旋转。
        /// </summary>
        public static void SendEventMessage(Vector3 position, Quaternion rotation)
        {
            var msg = new PlayerFireBullet();
            msg.Position = position;
            msg.Rotation = rotation;
            UniEvent.SendMessage(msg);
        }
    }
}
