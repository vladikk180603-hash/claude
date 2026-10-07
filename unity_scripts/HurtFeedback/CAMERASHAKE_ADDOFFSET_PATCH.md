# Вставка метода AddOffset в HitFeel CameraShake (НЕ замена файла)

Пакет HurtFeedback вызывает `CameraShake.AddOffset(Vector3, Vector3)`, чтобы наклонять камеру
в сторону удара. Это единственное изменение HitFeel. Файл `Assets/Scripts/HitFeel/Runtime/CameraShake.cs`
**не заменять** — в нём могут быть ваши правки. Нужно ВСТАВИТЬ три куска в существующий файл.
Если метод `AddOffset(Vector3, Vector3)` там уже есть — ничего не делать.

## 1. Поля (рядом с остальными private-полями класса)
```csharp
        // Внешняя добавка (наклон от урона и т.п.). Собирается за кадр и сбрасывается.
        private Vector3 _extraPos;
        private Vector3 _extraEuler;
        private bool _extraDirty;
```

## 2. Публичный метод (в любом месте класса)
```csharp
        // Добавка к позиции/повороту на ЭТОТ кадр (локальные оси). Вызывать каждый кадр из Update. Складывается.
        public void AddOffset(Vector3 localPositionOffset, Vector3 localEulerOffset)
        {
            _extraPos += localPositionOffset;
            _extraEuler += localEulerOffset;
        }
```

## 3. Применение в LateUpdate
Смысл: добавка должна применяться **даже когда тряски нет**, и после применения обнуляться.

В исходной версии HitFeel `LateUpdate` начинается с `if (_strength <= 0f) { return; }`
и в конце пишет `transform.localPosition = ...` / `transform.localRotation = ...`.
Нужно:

а) заменить ранний выход на:
```csharp
            bool hasExtra = _extraPos != Vector3.zero || _extraEuler != Vector3.zero;
            if (_strength <= 0f && !hasExtra)
            {
                if (_extraDirty)
                {
                    transform.localPosition = _baseLocalPos;
                    transform.localRotation = _baseLocalRot;
                    _extraDirty = false;
                }
                return;
            }
```
б) расчёт тряски (шум, offset, крен) выполнять только при `_strength > 0f`, иначе offset = 0 и крен = 0;
в) итоговую запись сделать такой (offset и roll — то, что посчитала тряска):
```csharp
            transform.localPosition = _baseLocalPos + offset + _extraPos;
            transform.localRotation = _baseLocalRot * Quaternion.Euler(_extraEuler.x, _extraEuler.y, _extraEuler.z + roll);
            _extraDirty = true;
            _extraPos = Vector3.zero;
            _extraEuler = Vector3.zero;
```
г) старый блок в конце `if (_strength <= 0f) { transform.localPosition = _baseLocalPos; ... }` удалить
(его роль выполняет _extraDirty).

д) в `OnDisable` добавить обнуление: `_extraPos = Vector3.zero; _extraEuler = Vector3.zero; _extraDirty = false;`

## Проверка
- F7/F5 (PlayerHurtDebug) — камера наклоняется в сторону удара и возвращается, тряска как раньше.
- Удары мечом (HitFeel) трясут камеру как раньше.

## Если вставить не получается
В `HurtFeedback/Runtime/HurtCameraKick.cs` закомментировать одну строку с `cs.AddOffset(...)` в `Update`.
Тряска останется, пропадёт только наклон в сторону удара.
