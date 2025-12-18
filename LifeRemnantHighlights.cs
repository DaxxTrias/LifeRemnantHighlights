using System.Numerics;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace RemnantHighlights
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

    public class RemnantHighlightsCore : BaseSettingsPlugin<Settings>
    {

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
            foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Effect])
            {
                var animatedComponent = entity.GetComponent<Animated>();
                if (animatedComponent == null) continue;

                var baseAnimatedObjectEntity = animatedComponent.BaseAnimatedObjectEntity;
                if (baseAnimatedObjectEntity == null) continue;
                
                var path = baseAnimatedObjectEntity.Path;
                if (string.IsNullOrEmpty(path)) continue;

                if (!path.Contains("skill_infusion", StringComparison.OrdinalIgnoreCase)) continue;

                var pos = entity.Pos;
                pos.Z += Settings.AxisOffset;

                Graphics.DrawCircleInWorld(pos, Settings.Radius, Settings.InfusionRemnantColor, Settings.Thickness, Settings.Smoothness);
            }
        }
    }
}
