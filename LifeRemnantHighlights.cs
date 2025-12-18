using System.Numerics;
using System.Reflection;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace LifeRemnantHighlights
{
    public class Settings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        
        
        public ToggleNode LifeRemnantEnable { get; set; } = new ToggleNode(true);
        public ToggleNode InfusionRemnantEnable { get; set; } = new ToggleNode(true);

        [Menu("Radius", "Radius of the circle.")]
        public RangeNode<int> Radius { get; set; } = new RangeNode<int>(25, 1, 100);

        [Menu("Thickness", "Thickness of the circle.")]
        public RangeNode<int> Thickness { get; set; } = new RangeNode<int>(2, 1, 20);

        [Menu("Smoothness", "Smoothness of the circle. Higher values have more impact on performance.")]
        public RangeNode<int> Smoothness { get; set; } = new RangeNode<int>(30, 1, 100);

        [Menu("Z-Axis offset", "How far to offset the circle on the z-axis. Probably don't need to touch this.")]
        public RangeNode<int> AxisOffset { get; set; } = new RangeNode<int>(0, -200, 200);

        public ColorNode LifeRemnantColor { get; set; } = new ColorNode(System.Drawing.Color.White);
        public ColorNode InfusionRemnantColor { get; set; } = new ColorNode(System.Drawing.Color.Blue);
    }

    public class LifeRemnantHighlightsCore : BaseSettingsPlugin<Settings>
    {
        private readonly Dictionary<long, Entity> _monsterToEffectMap = new();
        private List<Entity> _unmatchedMonsters = new();
        private List<Entity> _unmatchedEffects = new();
        private List<Entity> _cachedMonsters = new();
        private List<Entity> _cachedEffects = new();
        private DateTime _lastCacheTime = DateTime.MinValue;
        private const int CacheDurationMs = 100;

        public override void Render()
        {
            if (!Settings.Enable) return;
            
            if (Settings.LifeRemnantEnable) {
                DrawLifeRemnants();
            }
            
            if (Settings.InfusionRemnantEnable) {
                DrawInfusionRemnants();
            }
        }

        private void DrawLifeRemnants()
        {
            foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Effect])
            {
                var animatedComponent = entity.GetComponent<Animated>();
                if (animatedComponent == null) continue;

                var baseAnimatedObjectEntity = animatedComponent.BaseAnimatedObjectEntity;
                if (baseAnimatedObjectEntity?.Path == null) continue;

                if (!baseAnimatedObjectEntity.Path.Contains("blood_liferemnants")) continue;

                var pos = entity.Pos;
                pos.Z += Settings.AxisOffset;

                Graphics.DrawCircleInWorld(pos, Settings.Radius, Settings.LifeRemnantColor, Settings.Thickness, Settings.Smoothness);
            }
        }

        private void DrawInfusionRemnants()
        {
            if ((DateTime.Now - _lastCacheTime).TotalMilliseconds > CacheDurationMs)
            {
                _cachedMonsters = GetExposeSoulMonsters();
                _cachedEffects = GetInfusionRemnantEffects();
                _lastCacheTime = DateTime.Now;
            }

            var currentMonsters = _cachedMonsters;
            var currentEffects = _cachedEffects;

            // Clean up pairings if monster/effect invalid or pickup detected
            foreach (var (monsterId, effect) in _monsterToEffectMap.ToList())
            {
                var monster = currentMonsters.FirstOrDefault(m => m.Id == monsterId);
                if (monster is not { IsValid: true })
                {
                    _monsterToEffectMap.Remove(monsterId);
                    continue;
                }

                if (!effect.IsValid || !currentEffects.Contains(effect))
                {
                    _monsterToEffectMap.Remove(monsterId);
                    continue;
                }

                var effAnimated = effect.GetComponent<Animated>();
                if (effAnimated?.BaseAnimatedObjectEntity == null)
                {
                    _monsterToEffectMap.Remove(monsterId);
                }
            }

            _unmatchedMonsters = currentMonsters.Where(m => !_monsterToEffectMap.ContainsKey(m.Id)).ToList();
            _unmatchedEffects = currentEffects.Where(e => !_monsterToEffectMap.ContainsValue(e)).ToList();

            // Match effects to monsters by proximity
            foreach (var effect in _unmatchedEffects.ToList())
            {
                var effPos = effect.Pos;
                Entity? bestMonster = null;
                var bestDistance = float.MaxValue;

                foreach (var monster in _unmatchedMonsters)
                {
                    var dist = Vector3.DistanceSquared(effPos, monster.Pos);
                    if (!(dist < bestDistance)) continue;
                    
                    bestDistance = dist;
                    bestMonster = monster;
                }

                if (bestMonster == null) continue;
                _monsterToEffectMap[bestMonster.Id] = effect;
                _unmatchedMonsters.Remove(bestMonster);
                _unmatchedEffects.Remove(effect);
            }

            // Draw circles for matched monsters
            foreach (var kvp in _monsterToEffectMap)
            {
                var monsterId = kvp.Key;
                var monster = currentMonsters.FirstOrDefault(m => m.Id == monsterId);
                if (monster == null) continue;

                var pos = monster.Pos;
                pos.Z += Settings.AxisOffset - 85;
                Graphics.DrawCircleInWorld(pos, Settings.Radius, Settings.InfusionRemnantColor, Settings.Thickness, Settings.Smoothness);
            }
        }

        private List<Entity> GetExposeSoulMonsters()
        {
            var list = new List<Entity>();
            foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
            {
                var animatedComponent = entity.GetComponent<Animated>();
                if (animatedComponent == null) continue;

                var baseAnimatedObjectEntity = animatedComponent.BaseAnimatedObjectEntity;
                if (baseAnimatedObjectEntity == null) continue;

                var effectPackComponent = baseAnimatedObjectEntity.GetComponent<EffectPack>();
                if (effectPackComponent == null) continue;

                var effectsProperty = typeof(EffectPack).GetProperty("Effects", BindingFlags.NonPublic | BindingFlags.Instance);
                var effectsValue = effectsProperty?.GetValue(effectPackComponent);

                if (effectsValue is List<EffectPack.Effect> monsterEffects && monsterEffects.Any(e => e.Name.Contains("exposeSoul")))
                {
                    list.Add(entity);
                }
            }

            return list;
        }

        private List<Entity> GetInfusionRemnantEffects()
        {
            var list = new List<Entity>();
            foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Effect])
            {
                var animatedComponent = entity.GetComponent<Animated>();
                if (animatedComponent == null) continue;

                var baseAnimatedObjectEntity = animatedComponent.BaseAnimatedObjectEntity;
                if (baseAnimatedObjectEntity?.Path == null) continue;

                if (baseAnimatedObjectEntity.Path.Contains("skill_infusion"))
                {
                    list.Add(entity);
                }
            }
            return list;
        }
    }
}
