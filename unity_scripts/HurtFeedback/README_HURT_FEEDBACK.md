# HurtFeedback — игрок ясно чувствует, что его ударили (Unity 6, URP, от первого лица)

Эффекты срабатывают **в момент получения урона** и ничего не подсказывают заранее (никаких замахов-предупреждений).
Всё локально — видит только пострадавший игрок. Урон и неуязвимость решает хост, пакет только показывает.
`Time.timeScale` и Cinemachine не используются. Внешние картинки не нужны — маски рисуются кодом.

## Файлы
```
Assets/Scripts/HurtFeedback/
├── README_HURT_FEEDBACK.md
├── CAMERASHAKE_ADDOFFSET_PATCH.md     — что ВСТАВИТЬ в HitFeel CameraShake (единственное изменение HitFeel)
├── Editor/HurtFeedbackMenu.cs         — меню создания конфига
└── Runtime/
    ├── PlayerHurtFeedbackConfig.cs    — все числа (ScriptableObject)
    ├── IPlayerDamageFeedback.cs       — интерфейс: OnPlayerDamaged / SetHealth / ShowInvulnerability
    ├── PlayerHurtFeedback.cs          — координатор (ставится на игрока, всё остальное создаёт сам)
    ├── HurtHud.cs                     — создаёт экранный Canvas с эффектами
    ├── HurtTextures.cs                — рисует кодом виньетку и дугу
    ├── HurtVignette.cs                — a) красная виньетка + пульс при малом здоровье
    ├── HurtDirectionIndicators.cs     — b) дуги направления удара
    ├── HurtCameraKick.cs              — c) тряска и наклон через CameraShake
    ├── HurtBodyReaction.cs            — d) отброс назад и «замирание» ввода
    ├── HurtAudio.cs                   — e) звук удара, вскрик, сердце, приглушение
    ├── HurtHealthBar.cs               — f, g) полоска здоровья: дёрганье, белый кусок, мигание неуязвимости
    ├── PlayerHealthStub.cs            — заглушка здоровья: показывает, где подключать
    └── PlayerHurtDebug.cs             — F5–F9 для проверки без врагов
```

## Установка по шагам
1. Скопируйте папку `HurtFeedback` в `Assets/Scripts/`. Папки HitFeel и SwordCombo не трогайте.
2. Попросите агента сделать вставку из `CAMERASHAKE_ADDOFFSET_PATCH.md` в ваш `CameraShake.cs`
   (без этого Unity покажет ошибку «CameraShake does not contain a definition for AddOffset»).
3. Меню **Tools → Egg Game → Hurt Feedback → Create Config**. Появится `Assets/Scripts/HurtFeedback/Config/PlayerHurtFeedbackConfig`.
4. Выделите **корень игрока** (объект с CharacterController) → **Add Component**:
   - `PlayerHurtFeedback`: *Config* — перетащите созданный конфиг; *Player Camera* — камеру игрока
     (если пусто — берётся Main Camera); *Is Local Player* — включено.
   - `PlayerHurtDebug` — для проверки.
   - `PlayerHealthStub` — ТОЛЬКО если в проекте ещё нет своего здоровья игрока.
5. Звуки: положите аудиофайлы в `Assets/Audio/Hurt/` (любая папка подойдёт). В конфиге перетащите их в
   *Impact Clips* (удар в тело, 2–4 штуки), *Scream Clips* (вскрики, 2–4 штуки), *Heartbeat Clip* (стук сердца, зацикленный).
   Без клипов всё остальное работает, просто тихо.
6. Play → F5/F6/F7/F8. Экранный Canvas `[HurtHUD]` появится сам.

### Отброс и «замирание» (d)
По умолчанию отброс двигает игрока через **`CharacterController.Move`** (если его нет — Rigidbody, иначе transform).
«Замирание» — это `PlayerHurtFeedback.Local.InputMultiplier` (0 на 0.06 с, иначе 1). Ваш скрипт движения
должен умножать свой ввод на это число — это одна строка, её добавит агент. Если скрипт движения сам
управляет CharacterController и отброс конфликтует, включите на `HurtBodyReaction` *External Controller Mode*
и прибавляйте `CurrentKnockbackVelocity` к скорости в своём скрипте.

### Подключение к здоровью
Код здоровья игрока на клиенте пострадавшего должен вызвать:
```csharp
var fb = PlayerHurtFeedback.Local;           // или ссылка на компонент
fb.SetHealth(current, max);                  // после любого изменения здоровья (урон, лечение, возрождение)
fb.OnPlayerDamaged(damage, max, attackerPos); // при уроне: attackerPos — позиция ударившего
fb.ShowInvulnerability(0.5f);                 // если хост сообщил о неуязвимости (или включите авто в конфиге)
```
Пример — `PlayerHealthStub.ApplyDamage` (место помечено `>>> ПОДКЛЮЧЕНИЕ <<<`).
Для Fusion: хост считает урон и шлёт пострадавшему RPC `(damage, maxHealth, attackerPos)` → там `OnPlayerDamaged`;
здоровье синхронизируется [Networked]-свойством → при изменении `SetHealth`.

## Интенсивность
`intensity = clamp(damage / maxHealth × K, minIntensity, 1)`. При K = 4, min = 0.4:
гоблин 10 из 100 → 0.4 (умеренно, но заметно), удар 20 → 0.8, громила 25+ → 1.0 (максимум).
Каждый эффект умножается на intensity и на свой множитель из конфига (0..1).

## Защита от перегруза
- Тряска, наклон камеры, отброс и замирание — не чаще раза в `effectMinInterval` (0.15 с).
- Виньетка складывается, но не ярче `vignetteCap` (0.85).
- Дуг не больше `indicatorMaxCount` (4), новая заменяет самую старую.
- Вскрик не чаще раза в `screamCooldown` (0.8 с), звук удара — каждый раз.

## Значения конфига (по умолчанию)
| Группа | Поле | Значение | Что делает |
|---|---|---|---|
| Общее | intensityK | 4 | множитель формулы интенсивности |
| | minIntensity | 0.4 | минимум для слабого удара |
| | lowHealthThreshold | 0.3 | порог «мало здоровья» (30%) |
| | defaultMaxHealth | 100 | если код не передал макс. здоровье |
| | effectMinInterval | 0.15 с | ограничение тяжёлых эффектов |
| a) Виньетка | vignetteMultiplier | 1 | общий множитель |
| | vignetteColor | (0.75, 0, 0) | красный |
| | vignetteMaxAlpha | 0.7 | яркость при intensity 1 |
| | vignetteAttack / vignetteFade | 0.05 / 0.4 с | вспышка / затухание |
| | vignetteCap | 0.85 | потолок при серии |
| | vignetteInnerRadius | 0.5 | размер чистого центра |
| | lowHealthPulseAlpha / Rate | 0.25 / 1 раз в с | пульс при <30% |
| b) Дуги | indicatorMultiplier | 1 | |
| | indicatorLifetime | 1 с | |
| | indicatorMaxCount | 4 | |
| | indicatorRadius / Thickness | 160 / 18 px | при 1920×1080 |
| | indicatorArcDegrees | 70° | ширина дуги |
| | indicatorMinAlpha | 0.55 | прозрачность от слабого удара |
| c) Камера | cameraMultiplier | 1 | |
| | shakePosAmplitude | 0.04 м | |
| | shakeRollNoiseDeg | 1° | |
| | shakeRecoil | 0.03 м | |
| | shakeDuration / Frequency | 0.2 с / 25 | |
| | tiltRollDeg | 4° | наклон в сторону удара |
| | tiltPitchDeg | 2.5° | кивок от удара спереди/сзади |
| | tiltDuration | 0.3 с | |
| d) Тело | bodyMultiplier | 1 | |
| | knockbackDistance / Duration | 0.3 м / 0.12 с | |
| | freezeSeconds | 0.06 с | |
| | freezeInputMultiplier | 0 | ввод во время замирания |
| e) Звук | audioMultiplier | 1 | |
| | impactVolume / screamVolume | 0.9 / 0.8 | |
| | screamCooldown | 0.8 с | |
| | heartbeatVolume | 0.6 | |
| | lowHealthLowPassHz | 1200 Гц | приглушение при <30% |
| | mixer / mixerLowPassParam | пусто / LowPassCutoff | необязательно |
| f) Неуязвимость | invulnerabilitySeconds | 0.5 с | только показ |
| | showInvulnerabilityOnDamage | вкл | показывать сразу при уроне |
| | invulnerabilityTint / BlinkRate | розово-красный / 10 Гц | |
| g) Полоска | healthBarMultiplier | 1 | |
| | createHealthBar | вкл | выключить, если есть своя |
| | healthBarSize / Offset | 420×24 / (40, 40) | левый нижний угол |
| | healthBarShakePixels / Duration | 8 px / 0.25 с | |
| | lostChunkHold / Slide | 0.3 / 0.5 с | белый кусок |

Хочу сильнее — увеличьте `intensityK` и `minIntensity`. Хочу тише — уменьшайте множители групп (0..1).

## Чек-лист проверки
1. **F5** (10 урона слева): красная вспышка по краям, дуга слева, камера наклоняется влево, лёгкий толчок вправо,
   звук удара + вскрик, полоска дёргается, белый кусок стоит ~0.3 с и съезжает, полоска полсекунды мигает.
2. **F6** (справа) — всё зеркально. **F7** (сзади) — дуга внизу, камера кивает вниз, толчок вперёд.
3. **F8** (30 урона спереди): всё заметно сильнее, дуга вверху, толчок назад.
4. Поверните камеру после удара — дуга продолжает указывать на место удара и гаснет за ~1 с.
5. **Здоровье < 30%** (нажмите F8 три раза): виньетка пульсирует ~1 раз в секунду, звук глуше, слышно сердце.
   **F9** — вылечить: пульс, сердце и приглушение исчезают.
6. **Серия быстрых ударов** (быстро жмите F5/F6/F7): экран не становится сплошным красным, камера не «болтается»,
   дуг не больше 4, вскрик не на каждый удар.
7. **Гоблин спереди / сбоку / сзади** (после подключения урона): дуга указывает на гоблина, наклон и толчок от него.
8. Time.timeScale всё время = 1, остальные игроки ничего не видят.

## Что должен проверить локальный агент
1. **Здоровье игрока**: найти существующий код здоровья и добавить вызовы `SetHealth` / `OnPlayerDamaged`
   (позиция атакующего — позиция гоблина, а не точка удара по игроку). Если кода нет — `PlayerHealthStub`.
2. **CameraShake**: вставить `AddOffset` по `CAMERASHAKE_ADDOFFSET_PATCH.md` (не заменять файл).
   Проверить, что `CameraShake.Local` указывает на камеру локального игрока и что метод `Impulse(float, float, float, float, float, Vector3)` существует.
3. **Движение игрока**: умножить ввод на `PlayerHurtFeedback.Local.InputMultiplier`; убедиться, что отброс
   через CharacterController.Move не конфликтует (иначе External Controller Mode).
4. **Звук**: есть ли звуковая система/AudioMixer. Если есть миксер — открыть (Expose) параметр Cutoff freq у
   Lowpass-эффекта на нужной группе, назвать `LowPassCutoff` и задать миксер в конфиге. Иначе фильтр
   ставится на AudioListener автоматически.
5. **UI-канвас**: `[HurtHUD]` (Screen Space Overlay, sorting 500) не перекрывает меню паузы/инвентарь
   некрасиво; клики не перехватывает. Если в проекте уже есть полоска здоровья — `createHealthBar` выключить
   и при желании повторить эффекты дёрганья/белого куска на своей полоске.
6. **Сеть**: `PlayerHurtFeedback.isLocalPlayer` только у своего игрока (по `HasInputAuthority`);
   `OnPlayerDamaged` вызывать на клиенте пострадавшего по RPC от хоста; неуязвимость решает хост.
7. **asmdef** (если есть): HurtFeedback ссылается на сборку HitFeel, Unity.InputSystem и UnityEngine.UI.
