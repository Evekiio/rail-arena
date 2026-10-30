# Standalone rendered-frame evidence

Actual Windows player, RTX 2080 / Direct3D12, 1920x1080, High tier, one automated human and three bots. Hidden swapchains do not reliably render; these views force offscreen camera rendering and synchronize a one-pixel GPU readback each frame. Frame times include that readback overhead. Each view has 60 warmup frames followed by 180 samples. The five exported images are checked for non-black content and inspected directly.

Measurement version 2 stages the scenic observer after the player camera and before portal rendering, then renders the main view at EndOfFrame. Earlier reports staged portals from the player pose and are not equivalent portal workloads. Consult the report's measurementVersion and scope before comparing milestones. The separate fixed-step attribution profile measures uncapped throughput, not native frame pacing.

GPU timers returned no valid samples, so isolated GPU cost is unavailable. Overlay HUD, native swapchain/input/audio and sustained visible play remain Sanders' QA gate. Automated runs used -noaudio. Earlier black-swapchain readings are discarded.
