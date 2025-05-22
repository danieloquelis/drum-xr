# 🥁 DrumXR – Mixed Reality Drum Practice with Real-Time Feedback

![DrumXR Portrait](./drumxr_portrait.png)

**DrumXR** turns your **real acoustic drum kit** into an immersive learning experience using mixed reality and AI. Designed for Meta Quest, DrumXR blends physical drumming with spatial visual guidance to improve timing, rhythm, and technique — especially for beginners.

🎬 **Demo Video:**  
[Watch on YouTube](https://youtu.be/IjbBZS-Sz-A)

---

## 🚀 Features

- 🎯 **Spatial Note Overlay** – Real-time guidance using Guitar Hero–style falling notes aligned with your real drums
- 🧠 **AI Drum Kit Detection** – Automatically detects and classifies your drum parts using a Roboflow-trained model
- ✋ **Hand & Gesture Tracking** – No controllers required. Uses hand tracking to detect hits
- 👁️ **Passthrough View** – Practice while seeing your real drum kit via Meta's Passthrough Camera API
- 🔊 **Audio Detection** – Detect and score your hit accuracy in real time
- 🌐 **UnityWebRequest Integration** – Connects to cloud-hosted AI models for object detection

---

## 🧰 Tech Stack

| Component               | Version / Tool                                                         |
| ----------------------- | ---------------------------------------------------------------------- |
| Unity                   | `6000.0.23f1`                                                          |
| Meta XR SDK             | `v76`                                                                  |
| Passthrough Camera API  | Meta Samples                                                           |
| AI Model                | [View on Roboflow](https://universe.roboflow.com/drum-detection-zyt48) |
| Hand + Gesture Tracking | Meta Quest Native                                                      |
| Networking              | UnityWebRequest (HTTPS)                                                |

---

## 🧪 Setup Instructions

1. Clone this repository:

   ```bash
   git clone https://github.com/danieloquelis/drum-xr.git
   ```

2. Download [Passthrough Camera API Samples](https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples) from Meta's official GitHub repository.

3. Place the **PassthroughCameraApiSamples** folder inside your project's `/Assets` directory.

4. Import the latest **Meta XR SDK (v76)** via Unity Package Manager or from Meta's developer portal (I used all in one SDK).

5. Make sure your Unity version is `6000.0.23f1`.

6. Open the `DrumScanScene` and replace the `RoboflowInference` prefabe values with your APIKeys and model.

---

## 📁 Project Structure

```
DrumXR/
├── Assets/
│   ├── PassthroughCameraApiSamples/    # Meta's Passthrough Camera API
│   ├── Scripts/                        # Core game scripts
│   ├── Models/                         # 3D models and assets
│   ├── Materials/                      # Materials and textures
│   └── Scenes/                         # Unity scenes
├── Packages/                           # Unity packages
└── ProjectSettings/                    # Unity project settings
```

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📧 Contact

For any questions or feedback, please reach out to [daniel.oquelis@gmail.com](mailto:daniel.oquelis@gmail.com).
