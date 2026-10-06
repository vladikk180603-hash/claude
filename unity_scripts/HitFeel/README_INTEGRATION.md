# HitFeel — «сочные» удары ближнего боя (Unity 6, URP)

Пакет добавляет к удару мечом: замирание анимации (Hit Stop), отталкивание врага,
искры, тряску камеры, рывок вперёд и звук. **Урон пакет не наносит** — он только
сообщает «попал вот сюда», а ваша система урона подписывается на это событие.

Всё играется **только у того игрока, который ударил**. `Time.timeScale` не используется,
поэтому в мультиплеере (Photon Fusion 2) у остальных игроков игра не останавливается.

## 1. Установка

Скопируйте папку `HitFeel` целиком в `Assets/Scripts/HitFeel`. Папка `Editor` должна
остаться внутри и называться именно `Editor`. Существующие файлы проекта не меняются.

## 2. Создаём профили

Меню **Tools → Egg Game → Hit Feel**:
- **Create Sword Profile** — обычный меч (`Sword_HitFeel`);
- **Create Heavy Profile** — тяжёлое оружие (дольше замирание, отлёт 2 м, сильнее тряска);
- **Create Light Profile** — лёгкое быстрое оружие.

Профили появятся в `Assets/Scripts/HitFeel/Profiles`. Перетащите в поле **Sparks Prefab**
любой эффект искр (ParticleSystem). Можно оставить пустым — тогда искр не будет.

## 3. Что куда ставить

### Камера (тряска)
Тряска двигает **отдельный пустой объект**, а не саму камеру и не объект, который
поворачивает ваш скрипт обзора мышью. Иерархия:

```
Player                      ← HitFeelController, LungeMotion (+ CharacterController)
└── CameraPivot             ← ваш скрипт обзора крутит ЭТОТ объект (как и раньше)
    └── CameraShakeRoot     ← НОВЫЙ пустой объект, позиция/поворот 0 → CameraShake
        └── Main Camera     ← камера (перенесите её сюда)
            └── Руки/Оружие (Animator) ← HitFeelAnimationEvents
                └── Sword   ← Collider (Is Trigger) + MeleeHitbox
```

1. Правый клик по объекту, где сейчас лежит камера → **Create Empty**, назовите `CameraShakeRoot`.
2. В Transform поставьте Position 0,0,0 и Rotation 0,0,0.
3. Перетащите камеру внутрь `CameraShakeRoot`.
4. **Add Component → CameraShake**. Галочка *Register As Local* = вкл.
5. ВАЖНО для сети: CameraShake должен быть активен только у локального игрока
   (у чужих игроков камера обычно выключена — тогда и CameraShake выключите).

### Меч
1. На меч добавьте коллайдер (Box или Capsule), поставьте **Is Trigger**, растяните по клинку.
2. **Add Component → MeleeHitbox**.
   - *Attacker* — объект Player (если пусто, берётся корень иерархии).
   - *Contact Reference* — можно указать пустышку на середине клинка.
   - *Target Layers* — слой врагов.
   - Списки *Trail Renderers / Trail Particles / Trail Objects* — шлейф меча (необязательно).
3. Скрипт сам добавит на меч кинематический Rigidbody (нужен, чтобы срабатывал триггер).

### Объект с Animator рук/оружия
**Add Component → HitFeelAnimationEvents** и укажите в поле *Hitbox* ваш меч.
Нужен потому, что Unity вызывает Animation Events только на объекте с Animator.

### Игрок
- **Add Component → LungeMotion** (рывок). Если у вас свой контроллер движения,
  включите *External Controller Mode* и добавляйте `CurrentLungeVelocity` к своему движению.
- **Add Component → HitFeelController**: *Profile* = Sword_HitFeel, *Hitbox* = меч,
  *Weapon Animator* = Animator рук. *Is Local Player* — включено только у своего игрока.

### Враги
**Add Component → KnockbackReceiver**. Режим *Displacement* сам выберет способ
(NavMeshAgent → Rigidbody → Transform). *Strength Multiplier* — меньше для тяжёлых врагов.

## 4. Animation Events в клипе взмаха (мышью)

1. Выделите объект с Animator рук, откройте **Window → Animation → Animation**.
2. Выберите клип удара в выпадающем списке.
3. Ставите курсор времени (белая линия) на нужный кадр и жмёте кнопку
   **Add Event** (маленький значок с «+» у шкалы времени).
4. Кликните по появившейся метке и в инспекторе выберите функцию.

| Кадр | Функция |
|---|---|
| Начало замаха | `SwingStart` (рывок вперёд) и `TrailOn` |
| Клинок начинает рубить | `HitboxOn` |
| Клинок закончил рубить | `HitboxOff` |
| Конец удара | `TrailOff` |

Если клип импортирован из FBX (только чтение), события добавляйте в окне
импорта модели: вкладка **Animation → Events**.

## 5. Проверка без анимаций

Добавьте на игрока **HitFeelTester**, укажите *Profile*. В Play Mode:
- **H** — «удар» по тому, что перед камерой (до 3 м);
- **J** — рывок вперёд;
- **K** — только тряска камеры.

Работает и со старым Input Manager, и с новым Input System.
Окно удара мечом без анимации: вызовите `hitbox.StartSwingWithoutAnimation(0.1f, 0.15f)`.

## 6. Какие числа крутить

| Хочу | Что менять |
|---|---|
| Удар тяжелее | `hitStopSeconds` 0.08–0.1, `knockbackDistance` 1.8–2.5, `shakePosAmplitude` 0.05, `shakeRollAmplitudeDeg` 2–3, `shakeRecoilBack` 0.1 |
| Удар легче/быстрее | `hitStopSeconds` 0.03–0.04, `knockbackDistance` 0.5, `shakeDuration` 0.1, `shakeFrequency` 35 |
| Камеру укачивает | уменьшить `shakeRollAmplitudeDeg` и `shakeDuration` |
| Рывок слишком сильный | уменьшить `lungeDistance` |
| Рывок «ватный» | увеличить `lungeDamping` |

Hit Stop больше 0.15 с не бывает — это защитный лимит.

## 6.1. Внешние добавки к камере

`CameraShake.AddOffset(Vector3 localPos, Vector3 localEuler)` — добавка на один кадр
(вызывать из Update каждый кадр). Её использует пакет SwordCombo для покачивания при замахе.

## 7. Подключение к урону (пример для интегратора)

```csharp
hitbox.OnMeleeHit += info =>
{
    // info.target, info.contactPoint, info.direction
    // Здесь вызвать СУЩЕСТВУЮЩУЮ систему урона (RPC на хост в Fusion).
};
```
Событие `HitFeelController.OnHitFeelPlayed(HitInfo, HitFeelProfile)` — для звуковой системы.

## 8. Что должен проверить локальный агент

1. **Компиляция**: пакет писался без доступа к проекту — открыть Unity, Console без ошибок.
   Если в проекте есть свои `.asmdef`, а Input System отключён/нет пакета — проверить `HitFeelTester`.
2. **URP-материалы эффектов**: у префаба искр и шлейфов материалы на шейдерах
   `Universal Render Pipeline/Particles/Unlit` (иначе будут розовыми).
3. **Подписка на урон**: подписаться на `MeleeHitbox.OnMeleeHit` и вызвать существующую
   систему урона. Хит детектируется локально — урон должен подтверждать хост.
4. **Сеть (Fusion 2)**: `HitFeelController.isLocalPlayer` и `CameraShake` включать только
   при `Object.HasInputAuthority`. MeleeHitbox у чужих игроков отключить или не подписывать.
5. **Knockback в сети**: позицию врагов синхронизирует хост. Либо вызывать
   `KnockbackReceiver.Apply` на хосте (по RPC), либо оставить локально как чисто визуальный
   эффект — решить по тому, как враги синхронизируются (NetworkTransform перезапишет позицию).
6. **Animation Events**: имена функций совпадают (`HitboxOn`, `HitboxOff`, `TrailOn`, `TrailOff`, `SwingStart`).
7. **Слои**: `Target Layers` меча не включает слой игрока; у врагов есть не-триггер коллайдер.
8. **CameraShakeRoot** — отдельный объект, его не двигают другие скрипты.
