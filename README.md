# CRESSim: Simulator for Advancing Surgical Autonomy

CRESSim is another surgical simulator for the da Vinci Research Kit (dVRK) that enables simulating various contact-rich surgical tasks involving different surgical instruments, soft tissue, and body fluids. The real-world dVRK console and the master tool manipulator (MTM) robots are incorporated into the system to allow for teleoperation through virtual reality (VR).

**[Project Website](https://tbs-ualberta.github.io/CRESSim/)**

## Installation

Clone the repository. In the same directory, place the [PhysX 5 for Unity](https://github.com/yafei-ou/physx5-for-unity) package. Also, obtain the [Free Double Sided Shaders](https://assetstore.unity.com/packages/vfx/shaders/free-double-sided-shaders-23087) asset for your Unity account.

Open the project in Unity. Import the Free Double Sided Shaders assets from Package Manager.

## Citation

If you find this project helpful for your research, please consider citing the following two papers, which form the foundation of this repository.

```bibtex
@article{ou2024learning,
  title={Learning autonomous surgical irrigation and suction with the Da Vinci Research Kit using reinforcement learning},
  author={Ou, Yafei and Tavakoli, Mahdi},
  journal={arXiv preprint arXiv:2411.14622},
  year={2024}
}

@inproceedings{ou2024realistic,
  title={A Realistic Surgical Simulator for Non-Rigid and Contact-Rich Manipulation in Surgeries with the da Vinci Research Kit}, 
  author={Ou, Yafei and Zargarzadeh, Sadra and Sedighi, Paniz and Tavakoli, Mahdi},
  booktitle={2024 21st International Conference on Ubiquitous Robots (UR)}, 
  year={2024},
  pages={64-70}
}
```

