# Versitile Player Controller 2D for Unity

The following code project is created as a template to simplify getting your 2D Unity platformer up and running.
It is designed to allow you to configure, rather than code. Inspired by the platformers from the 80s and 90s.

This script will allow you to configure:
    - Player movement, such as movement speed, crouching and jump force.
    - Double Jumps
    - Dash
    - Wall Climbing/Sliding
    - Ground Pound
    - Camera controlls, such as if the camera is to follow the player, if it is to be smooth or frame a "stage" etc.
    - Hook in sprite controll to player state.

## Configuration

Adding this script to your GameObject will give you the following configuration options:
    - General Settings
        - Gravity                   (player gravity is isolated and disregards physic engine gravity)
        - Speed                     (movement speed)
        - Jump Force                (how well the player counters gravity when jumping)
        - Near Object Sensitivity   (when the player will "wallhug")

    - Camera Settings
        - Camera Follow X           (camera will follow you horizontally)
        - Camera Follow Y           (camera will follow you vertically)
        - Smooth Camera             (camera will follow smoothly with dampning enabled)
        - Camera Damping            (the magnitude of the damping when Smooth Camera is enabled)

    - Double Jump Settings
        - Use Double Jump           (Enable if you want to be able to jump many times in the air)
        - Max Double Jumps          (The max number of times your character can jump in the air)

    - Wall Climb Settings
        - Use Wall Climb            (Enable if you want to be able to wall climb)
        - Wall Slide Velocity       (Determines how fast you slide on a wall while hugging it)
        - Wall Jump Push Away Force (Determines how hard the player push away from the wall)
        - Wall Jump Away Duration   (Determines how ofter the player may make wall jumps)

    - Dash Settings
        - Use Dash                  (Enable if you want to be able to dash/boost)
        - Dash Force                (Determines how fast your character is while dashing and the force it renders)
        - Dash Duration             (Determines how long the player stays in a dash state)
        - Dash Locks Y              (Enalbe if the player will only be able to dash horizontally)
        - Dash Cooldown             (Determines how often the player can use dash)

    - Ground Pound Settings
        - Use Ground Pound          (Enable if you want to be able to ground pound)
        - Ground Pound Velocity     (Determines the velocity in which your character falls towards the ground while ground pounding)
        - Ground Pound Locks X      (Enable if you want the character to not move horizontally at all while using a ground pound)
        - Ground Pound Cooldown     (Determines how often the player can use a ground pound)


### Movement State

Movement state is an enum to define your characters current state.
Use this to transit between animation states or sprite sheets.

The script uses:
    - SpriteRenderer
    - Animator
As this was what the standard was when it was written. Check what standards apply to you and change these for animation controll if need be.

