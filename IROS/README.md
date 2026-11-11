# Learning Continuous Obstacle-Rich Pursuit for Drone Swarms

**IROS 2026 | DAM-DRL**

Project page for **Dual-stabilized Asynchronous Multi-stage Deep Reinforcement Learning (DAM-DRL)**.

This repository shares the core Unity scripts and prefabs associated with the work.

## Repository contents

| Directory | Contents |
| --- | --- |
| `Scripts/` | Unity C# source files |
| `Prefabs/` | Unity prefab assets |

This release contains the scripts and prefabs only. Unity scene files and a preconfigured Unity project are not included. Users need to assemble their own scene and configure the required dependencies and object references.

## Overview

Autonomous drone interception in obstacle-rich environments requires agents to respond to adaptive opponents while coordinating with teammates and avoiding collisions. In these competitive pursuit-evasion settings, strong opponents can suppress learning signals, while simultaneous policy updates can destabilize training.

DAM-DRL addresses these challenges through progressive multi-stage training and an alternating freeze-train-merge mechanism. The framework regulates adversarial co-evolution to support stable learning of pursuit and evasion policies.

## Method

The training framework combines three components:

1. **Dual cold-start:** establish initial policies for both chasers and runners under simplified conditions.
2. **Alternating freeze-and-train:** hold one team's policy fixed while training the opposing team.
3. **Policy merging:** combine the trained policies to initialize the next training stage.

Together, these components control the growth of adversarial strength across training stages.

## Experiments

The paper evaluates DAM-DRL in obstacle-rich drone pursuit-evasion simulations, with comparisons against representative learning-based and planning-based methods. Physical flight experiments further demonstrate deployment of the learned policies on real UAVs.

## Using the scripts and prefabs

1. Create a Unity project and install the packages required by the provided scripts.
2. Copy `Scripts/` and `Prefabs/` into the project's `Assets/` directory. Preserve the accompanying `.meta` files and directory structure.
3. Create a scene, add the required prefab instances, and configure the scene objects and Inspector references used by the scripts. Resolve any additional asset dependencies referenced by the prefabs.
4. If a component requires a trained policy, provide a compatible model and configure its inputs and actions to match the implementation.

<!-- Add the paper link, demonstration videos, method figure, and verified BibTeX citation as they become available. -->

## License

See [LICENSE](LICENSE) for the MIT license.

