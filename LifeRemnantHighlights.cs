using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace LifeRemnantHighlights
{
    public class Settings : ISettings
    {
        // left pane Enable toggle
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        
        [Menu("Radius", "Radius of the circle.")]
        public RangeNode<int> Radius { get; set; } = new RangeNode<int>(25, 1, 100);
        [Menu("Thickness", "Thickness of the circle.")]
        public RangeNode<int> Thickness { get; set; } = new RangeNode<int>(5, 1, 20);
        [Menu("Smoothness", "Smoothness of the circle. Higher values have more impact on performance.")]
        public RangeNode<int> Smoothness { get; set; } = new RangeNode<int>(30, 1, 100);
        [Menu("Z-Axis offset", "How far to offset the circle on the z-axis. Probably don't need to touch this.")]
        public RangeNode<int> AxisOffset { get; set; } = new RangeNode<int>(10, -20, 20);
        public ColorNode Color { get; set; } = new ColorNode(System.Drawing.Color.White);
    }
    
    public class LifeRemnantHighlightsCore : BaseSettingsPlugin<Settings>
    {
        public override void Render()
        {
            if (!Settings.Enable) return;
            foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Effect])
            {
                var animatedEntity = entity.GetComponent<Animated>();
                // ReSharper disable once UseNullPropagation
                if (animatedEntity == null) continue;
                
                var baseAnimatedObjectEntity = animatedEntity.BaseAnimatedObjectEntity;
                if (baseAnimatedObjectEntity.Path == null) continue;
                
                if (!baseAnimatedObjectEntity.Path.Contains("blood_liferemnants")) continue;
                
                var pos = entity.Pos;
                pos.Z += Settings.AxisOffset;
                
                Graphics.DrawCircleInWorld(pos, Settings.Radius, Settings.Color, Settings.Thickness, Settings.Smoothness);

            }
        }
    }
}