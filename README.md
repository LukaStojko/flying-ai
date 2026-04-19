# flying-ai

This project builds on the open-source [Aircraft-Physics](https://github.com/gasgiant/Aircraft-Physics/tree/master) framework, which simulates fixed-wing aerodynamics using multiple lifting surfaces rather than Unity’s default drag model.

---

## AI System

The AI is structured as a **state-driven controller**:

- **Patrol**  
  Fly between waypoints or loiter in an area

- **Maintain Altitude**  
  Continuous correction regardless of state

- **Engage / Attack**  
  Triggered when the player is detected

---

## Detection System

The detection system uses:

- Distance check  
- Field-of-view (**cone**)

---

## Cone-Based Target Navigation

A directional cone is used to evaluate whether the aircraft is flying correctly toward a target.

### Concept

- Define a cone in front of the aircraft  
- Compare direction to target vs forward vector  
- If the target is **inside the cone** → stay on course  
- If the target is **outside the cone** → apply corrective steering  

---

## Behavior Breakdown

### Inside Cone

- Maintain current heading  
- Apply small smoothing corrections  
- Increase throttle if far from target  

### Outside Cone

- Compute correction:
  - Pitch toward vertical difference  
  - Roll toward horizontal direction  
- Gradually steer instead of snapping  