# Learning Continuous Obstacle-Rich Pursuit for Drone Swarms

**IROS 2026 | DAM-DRL**

Project page for **Dual-stabilized Asynchronous Multi-stage Deep Reinforcement Learning (DAM-DRL)**.

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

## Repository status

This repository currently provides a research overview. Source code, trained models, and reproduction instructions have not yet been added.

<!-- Add the paper link, demonstration videos, method figure, and verified BibTeX citation as they become available. -->

## License

See [LICENSE](LICENSE) for the MIT license.

