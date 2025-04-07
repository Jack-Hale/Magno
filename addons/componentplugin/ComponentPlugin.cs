#if TOOLS
using Godot;
using System;

[Tool]
public partial class ComponentPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>("res://Source/Components/HealthComponent.cs");
        var icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/HealthComponentIcon.png");
        AddCustomType("HealthComponent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/MagneticComponent.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/MagneticComponentIcon.png");
        AddCustomType("MagneticComponent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/MagneticCharacterComponent.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/MagneticCharacterComponentIcon.png");
        AddCustomType("MagneticCharacterComponent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/MagneticCharacterParent.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/MagneticCharacterParentIcon.png");
        AddCustomType("MagneticCharacterParent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/DamageComponent.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/DamageComponentIcon.png");
        AddCustomType("DamageComponent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/ProjectileComponent.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/ProjectileComponentIcon.png");
        AddCustomType("ProjectileComponent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/PathFindingComponent.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/PathFindingComponentIcon.png");
        AddCustomType("PathFindingComponent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/ItemComponent.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/ItemComponentIcon.png");
        AddCustomType("ItemComponent", "Node2D", script, icon);

        script = GD.Load<Script>("res://Source/Components/ProjectileLauncher.cs");
        icon = GD.Load<Texture2D>("res://Assets/CustomNodeIcons/ProjectileLauncherIcon.png");
        AddCustomType("ProjectileLauncher", "Node2D", script, icon);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("HealthComponent");
        RemoveCustomType("MagneticComponent");
        RemoveCustomType("MagneticCharacterComponent");
        RemoveCustomType("MagneticCharacterParent");
        RemoveCustomType("DamageComponent");
        RemoveCustomType("ProjectileComponent");
        RemoveCustomType("PathFindingComponent");
        RemoveCustomType("ItemComponent");
        RemoveCustomType("ProjectileLauncher");
    }
}
#endif
