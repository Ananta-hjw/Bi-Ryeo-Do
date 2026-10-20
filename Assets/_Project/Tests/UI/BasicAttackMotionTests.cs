#if UNITY_EDITOR
using System;
using System.Reflection;
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

        [TestCase("basic", false, 1, true)]
        [TestCase("공격", false, 1, true)]
        [TestCase("basic", true, 1, false)]
        [TestCase("basic", false, 2, false)]
        [TestCase("samcheopyeong", false, 1, false)]
        public void OnlySingleBasicAttacksUseApproach(string skill, bool joint, int hits, bool expected)
        {
            var method = typeof(BattleScreen).GetMethod("IsBasicAttack", BindingFlags.NonPublic | BindingFlags.Static);
            var ev = new BattleEvent { Kind = EventKind.Attack, SkillId = skill, Joint = joint, HitCount = hits };
            Assert.AreEqual(expected, (bool)method.Invoke(null, new object[] { ev }));
        }
    }
}
#endif
