# Core Pooling

GameObject pooling theo prefab, namespace `ColonyFlow.Core.Pooling`. Assembly không phụ thuộc Tween/gameplay hay package mới.

- [Runtime](Runtime/README.md): contracts và state.
- [Docs](Docs/core-pooling-usage.md): cách dùng và tích hợp.

Manager sống theo owner scene; không singleton hoặc DontDestroyOnLoad. Recycle tự deactivate và chạy reset hook.
