#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class BasicAttackMotionTests
    {
        [TestCase(1f)]
        [TestCase(-1f)]
        public void ApproachCopiesOnlyFigureAndCancellationRestoresIt(float facing)
        {
            var root = new GameObject("Battle", typeof(RectTransform));
            try
            {
                var field = root.GetComponent<RectTransform>();
                field.sizeDelta = new Vector2(1080f, 1920f);
                var motion = root.AddComponent<BasicAttackMotion>();
                var slot = UiFactory.Rect("Slot", field, Vector2.zero, Vector2.zero);
                slot.sizeDelta = new Vector2(180f, 300f);
                var figure = UiFactory.Panel("Figure", slot, Vector2.zero, Vector2.zero, Color.white);
                figure.rectTransform.pivot = new Vector2(.5f, 0f);
                figure.rectTransform.sizeDelta = new Vector2(120f, 180f);
                figure.rectTransform.localScale = new Vector3(facing, 1f, 1f);
                var hp = UiFactory.Panel("Hp", slot, Vector2.zero, Vector2.one, Color.green);
                var target = UiFactory.Panel("Target", field, Vector2.one, Vector2.one, Color.white);
                target.rectTransform.sizeDelta = new Vector2(120f, 180f);
                var fx = UiFactory.Rect("Fx", field, Vector2.zero, Vector2.one);
                var position = figure.rectTransform.position;
                var rotation = figure.rectTransform.rotation;
                var scale = figure.rectTransform.localScale;
                var gaugePosition = hp.rectTransform.position;
                var approach = motion.Approach(figure, target, fx, 1);
                Assert.IsTrue(approach.MoveNext());
                Assert.IsTrue(motion.IsActive);
                Assert.IsFalse(figure.enabled);
                Assert.AreEqual(1, fx.childCount);
                var copy = fx.GetChild(0).GetComponent<Image>();
                Assert.IsNotNull(copy);
                Assert.IsFalse(copy.raycastTarget);
                Assert.AreEqual(position, copy.rectTransform.position);
                Assert.AreEqual(Mathf.Sign(facing), Mathf.Sign(copy.rectTransform.localScale.x));
                Assert.AreEqual(position, figure.rectTransform.position);
                Assert.AreEqual(gaugePosition, hp.rectTransform.position);
                motion.Cancel();
                Assert.IsTrue(figure.enabled);
                Assert.IsFalse(motion.IsActive);
                Assert.AreEqual(0, fx.childCount);
                Assert.AreEqual(position, figure.rectTransform.position);
                Assert.AreEqual(rotation, figure.rectTransform.rotation);
                Assert.AreEqual(scale, figure.rectTransform.localScale);
                Assert.DoesNotThrow(() => motion.Cancel());
                Assert.IsFalse(approach.MoveNext(), "cancelled approach must not resume another phase");
                (approach as IDisposable)?.Dispose();
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DisablingMotionRestoresFallbackTextAndMissingTargetDoesNothing()
        {
            var root = new GameObject("Battle", typeof(RectTransform));
            try
            {
                var field = root.GetComponent<RectTransform>();
                field.sizeDelta = new Vector2(1080f, 1920f);
                var motion = root.AddComponent<BasicAttackMotion>();
                var source = UiFactory.Rect("Glyph", field, Vector2.zero, Vector2.zero).gameObject.AddComponent<Text>();
                source.text = "비";
                var target = UiFactory.Panel("Target", field, Vector2.one, Vector2.one, Color.white);
                var layer = UiFactory.Rect("Fx", field, Vector2.zero, Vector2.one);
                Assert.IsFalse(motion.Approach(source, null, layer, 1).MoveNext());
                Assert.IsTrue(source.enabled);
                var approach = motion.Approach(source, target, layer, 3);
                Assert.IsTrue(approach.MoveNext());
                Assert.IsFalse(source.enabled);
                motion.enabled = false;
                Assert.IsTrue(source.enabled);
                Assert.IsFalse(motion.IsActive);
                Assert.AreEqual(0, layer.childCount);
                (approach as IDisposable)?.Dispose();
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void WholeUnitMovesItsFigureGaugesAndStatusTogetherAndCancelRestoresVisibility()
        {
            var root = new GameObject("Battle", typeof(RectTransform));
            try
            {
                var field = root.GetComponent<RectTransform>();
                field.sizeDelta = new Vector2(1080f, 1920f);
                var motion = root.AddComponent<BasicAttackMotion>();
                var unit = UiFactory.Rect("Unit", field, Vector2.zero, Vector2.zero);
                unit.sizeDelta = new Vector2(180f, 300f);
                var group = unit.gameObject.AddComponent<CanvasGroup>();
                group.alpha = .7f;
                var figure = UiFactory.Panel("Figure", unit, Vector2.zero, Vector2.one, Color.white);
                foreach (var name in new[] { "Hp", "Qi", "Spirit", "Status" })
                    UiFactory.Label(name, unit, Vector2.zero, Vector2.one, name, 24, Color.white);
                var target = UiFactory.Panel("Target", field, Vector2.one, Vector2.one, Color.white);
                var fx = UiFactory.Rect("Fx", field, Vector2.zero, Vector2.one);
                var originalPosition = unit.position;
                var approach = motion.Approach(figure, target, fx, 1, unit);
                Assert.IsTrue(approach.MoveNext());
                Assert.AreEqual(0f, group.alpha);
                var clone = (RectTransform)fx.GetChild(0);
                Assert.AreEqual(.7f, clone.GetComponent<CanvasGroup>().alpha);
                Assert.IsFalse(clone.GetComponent<CanvasGroup>().blocksRaycasts);
                foreach (var name in new[] { "Figure", "Hp", "Qi", "Spirit", "Status" }) Assert.IsNotNull(clone.Find(name));
                var hp = clone.Find("Hp");
                var hpPosition = hp.position;
                clone.anchoredPosition += new Vector2(100f, 40f);
                Assert.That(Vector3.Distance(hp.position, hpPosition), Is.GreaterThan(1f));
                Assert.AreEqual(originalPosition, unit.position);
                motion.Cancel();
                Assert.AreEqual(.7f, group.alpha);
                Assert.AreEqual(originalPosition, unit.position);
                Assert.AreEqual(0, fx.childCount);
                (approach as IDisposable)?.Dispose();
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TurnIndicatorUsesPlaybackActorUntilEventsFinishThenUsesNextInputMember()
        {
            var root = new GameObject("Battle", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<BattleScreen>();
                var first = new Combatant("first", "First", Team.Player,
                    new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 1, Defense = 1, Speed = 100 }, null);
                var next = new Combatant("next", "Next", Team.Player,
                    new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 1, Defense = 1, Speed = 95 }, null);
                var enemy = new Combatant("enemy", "Enemy", Team.Enemy,
                    new CombatStats { MaxHp = 10000, Attack = 1, Defense = 1, Speed = 1 }, null);
                var battle = new InteractiveBattle(new List<Combatant> { first, next }, new List<Combatant> { enemy }, new SeededRandom(1));
                battle.Begin();
                Assert.AreSame(first, battle.Current);
                battle.PlayerAct(PlayerCommand.Guard());
                Assert.AreSame(next, battle.Current, "the engine already moved to the next input member");
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(BattleScreen).GetField("_battle", flags).SetValue(screen, battle);
                typeof(BattleScreen).GetField("_animating", flags).SetValue(screen, true);
                var actor = typeof(BattleScreen).GetField("_hudActor", flags);
                var visible = typeof(BattleScreen).GetProperty("VisibleActor", flags);
                actor.SetValue(screen, first);
                Assert.AreSame(first, visible.GetValue(screen));
                actor.SetValue(screen, enemy);
                Assert.AreSame(enemy, visible.GetValue(screen), "enemy playback must not highlight the next ally");
                actor.SetValue(screen, null);
                Assert.IsNull(visible.GetValue(screen), "do not leak the next actor before playback starts");
                typeof(BattleScreen).GetField("_animating", flags).SetValue(screen, false);
                Assert.AreSame(next, visible.GetValue(screen));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("basic", false, 1, false, true)]
        [TestCase("공격", false, 1, false, true)]
        [TestCase("basic", true, 1, false, true)]
        [TestCase("basic", false, 2, false, true)]
        [TestCase("samcheopyeong", false, 1, false, true)]
        [TestCase("ultimate", false, 3, false, true)]
        [TestCase("area", false, 1, true, false)]
        [TestCase("area", false, 3, true, false)]
        public void EveryDirectAttackExceptAreaDamageUsesApproach(string skill, bool joint, int hits, bool area, bool expected)
        {
            var method = typeof(BattleScreen).GetMethod("IsApproachAttack", BindingFlags.NonPublic | BindingFlags.Static);
            var ev = new BattleEvent { Kind = EventKind.Attack, SkillId = skill, Joint = joint, HitCount = hits, AllEnemies = area };
            Assert.AreEqual(expected, (bool)method.Invoke(null, new object[] { ev }));
            ev.Kind = EventKind.Status;
            Assert.IsFalse((bool)method.Invoke(null, new object[] { ev }));
        }

        [Test]
        public void MultiHitMotionReturnsOnlyAfterTheLastActualHitIncludingStatusKills()
        {
            var method = typeof(BattleScreen).GetMethod("EndsAttackSequence", BindingFlags.NonPublic | BindingFlags.Static);
            var first = new BattleEvent { Kind = EventKind.Attack, ActorId = "p", TargetId = "e", SkillId = "art", HitIndex = 0, HitCount = 3 };
            var second = new BattleEvent { Kind = EventKind.Attack, ActorId = "p", TargetId = "e", SkillId = "art", HitIndex = 1, HitCount = 3 };
            var bleed = new BattleEvent { Kind = EventKind.Status, TargetId = "e", TargetDefeated = true };
            var reply = new BattleEvent { Kind = EventKind.Attack, ActorId = "other", TargetId = "p", HitIndex = 0 };
            var events = new List<BattleEvent> { first, second, bleed, reply };
            Assert.IsFalse((bool)method.Invoke(null, new object[] { events, 0 }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { events, 1 }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { events, 3 }));
        }
    }
}
#endif
