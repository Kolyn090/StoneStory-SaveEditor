if __name__ == "__main__":
    gears1 = [
        "socketed_staff",
        "socketed_crossbow",
        "socketed_shield",
        "socketed_hammer",
        "socketed_sword",
        "socketed_long_sword",
        "wand",
    ]
    elements = [
        "AEther",
        "Ice",
        "Vigor",
        "Poison",
        "Fire"
    ]
    gears2 = [
        "hammer",
        "heavy_hammer",
        "skeleton_arm",
        "tower_shield",
        "lollipop_wand",
        "sword",
        "crossbow",
        "shield",
        "dashing_shield",
        "bashing_shield",
        "bardiche",
        "cult_mask",
        "blade_of_god",
        "repeating_crossbow",
        "heavy_crossbow",
        "compound_shield",
        "quarterstaff",
        "aether_talisman:AEther",
        "fire_talisman:Fire",
    ]

    gears = []
    for g1 in gears1:
        for e in elements:
            gears.append(f"{g1}:{e}")
    gears.extend(gears1)
    gears.extend(gears2)
    gears_str = ",".join(gears)

    golden = f"golden:[{gears_str}]"
    prismatic = f"prismatic:[{gears_str}]"
    glitch = f"glitch:[{gears_str}]"
    extra = ["{c:#000000}"] * len(prismatic)
    result = f"cosmetics:{{{golden},{prismatic},{glitch},extra:[{",".join(extra)}]}}"
    print(result)
