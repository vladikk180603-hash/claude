# SwordCombo — серия из 3 ударов мечом от первого лица (Unity 6, URP)

Меч двигается кривыми из ScriptableObject — **без анимационных клипов и Animator Controller**.
Работает поверх пакета **HitFeel** (он должен лежать в `Assets/Scripts/HitFeel`).
`Time.timeScale` и Cinemachine не используются.

## Что внутри
```
Assets/Scripts/SwordCombo/
├── README_SWORD_COMBO.md
├── Editor/SwordComboMenu.cs        — меню: создать 3 профиля ударов и SwordPivot с мечом-заглушкой
└── Runtime/
    ├── SwingProfile.cs             — настройки одного удара (кривые, окно удара, множители, камера)
    ├── SwordComboController.cs     — ввод, состояния Idle/Swing/Recovery, серия, буфер, попадания
    ├── ComboHitFeelBridge.cs       — связь с HitFeel: эффекты с множителями + остановка замаха
    ├── ComboDamage.cs              — IDamageable (заглушка), DamageRequest, IDamageRequestSender
    ├── LocalDamageSender.cs        — отправка урона без сети (место подключения своей системы урона)
    ├── SwingCameraSway.cs          — покачивание камеры при замахе (свой объект, HitFeel не трогает)
    ├── SwordPlaceholder.cs         — меч-заглушка из кубов
    └── TrainingDummy.cs            — манекен для проверки
```
**Файлы HitFeel не меняются и не заменяются.** Пакет только вызывает публичные методы HitFeel.

## Установка по шагам

1. Скопируйте папку `SwordCombo` в `Assets/Scripts/` (папку HitFeel не трогайте). Дождитесь, пока Unity перекомпилирует, в Console не должно быть красных ошибок.
2. **Профили ударов:** меню **Tools → Egg Game → Sword Combo → Create 3 Swing Profiles**.
   В `Assets/Scripts/SwordCombo/Profiles` появятся `Swing1_LeftToRight`, `Swing2_RightToLeft`, `Swing3_Overhead`
   с готовыми кривыми. (Если пропустить этот шаг, контроллер использует те же 3 удара встроенными.)
3. **Меч:** в Hierarchy выделите камеру игрока → **Tools → Egg Game → Sword Combo → Create Sword Pivot Under Camera**.
   Над камерой появится `SwingSwayRoot` (покачивание), под камерой — `SwordPivot` с мечом-заглушкой. Near Clip Plane камеры станет 0.05 (иначе меч обрезается).
   Иерархия должна получиться такой:
   ```
   Player                       ← SwordComboController, ComboHitFeelBridge, HitFeelController, LungeMotion, LocalDamageSender
   └── ... CameraShakeRoot      ← CameraShake (из HitFeel, без изменений)
           └── SwingSwayRoot    ← SwingCameraSway (новый, покачивание от замаха)
               └── Main Camera
                   └── SwordPivot   ← пустой объект «рука»
                       └── SwordModel_Placeholder
   ```
4. Выделите **Player** → **Add Component**:
   - `SwordComboController`:
     - *Sword Pivot* — перетащите `SwordPivot`;
     - *Swings* — размер 3, перетащите три профиля по порядку;
     - *Hit Layers* — слой врагов (без слоя игрока!);
     - остальное оставьте по умолчанию.
   - `LocalDamageSender` (*Base Damage* = 10).
   - `ComboHitFeelBridge` (поля найдутся сами, если всё на одном объекте).
   - `HitFeelController` и `LungeMotion` — уже должны быть из HitFeel. В `HitFeelController` поле *Profile* =
     `Sword_HitFeel` (лучше `Heavy_HitFeel` — меч тяжёлый). Поле *Hitbox* оставьте пустым: попадания теперь
     считает SwordComboController. Если у вас был `MeleeHitbox` на мече — выключите его.
5. **Манекен:** создайте куб (GameObject → 3D Object → Cube), поставьте перед игроком, добавьте
   `TrainingDummy` и `KnockbackReceiver`. Слой — тот, что выбран в *Hit Layers*.
6. Play → левая кнопка мыши.

## Ввод
Выбран **автоматически**: если в Player Settings включён новый Input System (*Active Input Handling* = Input System
Package или Both) — используется `Mouse.current.leftButton`, иначе старый `Input.GetMouseButtonDown(0)`.
Чтобы управлять со своего ввода: снимите галочку *Read Mouse Input* и вызывайте `combo.RequestAttack()`.

## Три удара (значения, которые создаёт меню)

Кривые — смещение от стойки. Стойка: позиция (0.32, −0.38, 0.5), поворот (30, 0, 12).
Ключи кривых (время → значение), между ними — плавные касательные:

| Поле | Удар 1: слева направо | Удар 2: справа налево | Удар 3: сверху вниз |
|---|---|---|---|
| Duration | 0.55 | 0.60 | 0.85 |
| Время ключей | 0 / 0.25 / 0.42 / 0.62 / 1 | 0 / 0.25 / 0.42 / 0.62 / 1 | 0 / 0.35 / 0.50 / 0.68 / 1 |
| posX | 0 / −0.45 / −0.10 / 0.25 / 0.20 | 0 / 0.25 / 0 / −0.35 / −0.30 | 0 / −0.12 / −0.20 / −0.22 / −0.18 |
| posY | 0 / 0.18 / 0.12 / 0.08 / 0.02 | 0 / 0.10 / 0.06 / 0.02 / 0 | 0 / 0.38 / 0.15 / −0.12 / −0.10 |
| posZ | 0 / 0 / 0.15 / 0.10 / 0 | 0 / −0.05 / 0.15 / 0.10 / 0 | 0 / −0.05 / 0.20 / 0.20 / 0.10 |
| rotX | 0 / 55 / 60 / 60 / 50 | 0 / 55 / 62 / 62 / 50 | 0 / −50 / 40 / 115 / 105 |
| rotY | 0 / −80 / −20 / 55 / 70 | 0 / 85 / 25 / −55 / −70 | 0 / −10 / −10 / −8 / −5 |
| rotZ | 0 / −12 / −12 / −12 / −12 | 0 / −30 / −30 / −30 / −25 | 0 / −15 / −15 / −15 / −15 |
| Hit Window | 0.32 – 0.60 | 0.33 – 0.60 | 0.45 – 0.70 |
| Damage Multiplier | 1.0 | 1.2 | 1.8 |
| Hit Stop Multiplier | 1.0 | 1.15 | 1.6 |
| Knockback Multiplier | 0.8 | 1.0 | 1.7 |
| Shake Multiplier | 1.0 | 1.15 | 1.8 |
| Lunge Multiplier | 0.8 | 1.0 | 1.3 |
| Camera Roll Deg | −2 | 2.5 | 0 |
| Camera Pitch Deg | 0 | 0 | 3 |
| Camera Sway Pos | (0.02, 0, 0) | (−0.025, 0, 0) | (0, −0.03, 0.02) |
| Recovery Duration | 0.30 | 0.32 | 0.40 |

Настройки серии в `SwordComboController`: буфер ввода 0.4 с, сброс серии через 0.8 с, пауза после 3-го удара 0.45 с.

## Как это связано с HitFeel
- `SwordComboController` выдаёт события: `OnSwingStart(step)`, `OnImpact(HitInfo, step)`, `OnSwingEnd(step)`,
  отдельно `OnDamageRequested(DamageRequest)`.
- `ComboHitFeelBridge` подписан на `OnSwingStart` (рывок LungeMotion) и `OnImpact` (вызывает
  `HitFeelController.PlayHit` с профилем, умноженным на множители удара, и `combo.FreezeSwing(...)`).
- **Остановка замаха**: `FreezeSwing` ставит локальную скорость замаха `SwingSpeed` в 0 на время hitStop (максимум 0.2 с),
  потом возвращает 1. Нажатия в это время попадают в буфер.
- **Камера**: контроллер камеру не двигает. Крен/кивок/сдвиг замаха каждый кадр отправляется в
  `SwingCameraSway.AddOffset(...)` на объекте `SwingSwayRoot`. Тряска от попаданий остаётся в HitFeel `CameraShake`
  на `CameraShakeRoot`. Каждый скрипт двигает только свой объект, поэтому они не мешают друг другу и складываются.
  Если `SwingSwayRoot` не создавать — всё работает, просто без покачивания при замахе.

## Настоящая модель меча (когда появится)
- **Точка хвата = начало координат `SwordPivot`** (место, где кисть сжимает рукоять, сразу под гардой).
- Клинок должен смотреть вдоль **+Y** `SwordPivot`, режущая кромка — вдоль **±X**, плоскость клинка — X-Y.
- Шаги: положите модель внутрь `SwordPivot`, удалите `SwordModel_Placeholder`. У модели подберите:
  - *Position*: сдвиньте так, чтобы рукоять под гардой оказалась в (0, 0, 0);
  - *Rotation*: чтобы клинок смотрел вверх по +Y (часто это (−90, 0, 0) или (0, 0, 90) для FBX из Blender);
  - *Scale*: общая длина ≈ 1.3 м (рукоять ≈ 0.25, клинок ≈ 1.0).
- В `SwordComboController` поправьте *Blade Start* (основание клинка, сейчас 0.1) и *Blade End* (кончик, сейчас 1.1)
  или назначьте пустышки *Blade Base* / *Blade Tip* на модели. Жёлтые сферы в окне Scene (при выделенном игроке)
  показывают зону попадания.
- Коллайдеры на модели меча не нужны (удалите их).
- Если меч в стойке закрывает много экрана — меняйте *Stance Position* / *Stance Euler* в контроллере, а не кривые.

## Чек-лист проверки
1. **Удар 1**: клик — меч уходит влево и рубит слева направо (~0.55 с), манекен отлетает, в Console «Удар 1 … урон 10».
2. **Удар 2**: второй клик сразу после — справа налево, урон 12, камера наклоняется в другую сторону.
3. **Удар 3**: третий клик — меч поднимается и рубит сверху вниз, медленнее, урон 18, самый сильный отброс и тряска.
4. **Сброс серии**: ударьте один раз, подождите больше секунды, ударьте снова — снова «Удар 1».
5. **Пауза после 3-го**: сразу после 3-го удара клики почти не работают ~0.45 с, затем снова «Удар 1».
6. **Буфер**: нажмите во второй половине удара — следующий начнётся сам сразу после окончания текущего.
7. **Остановка кадра**: при попадании меч на миг замирает и продолжает с того же места, удар доходит до конца,
   игра у остальных не замирает (Time.timeScale = 1 всё время).
8. **Один враг — один урон за удар**: в Console одна строка на удар, даже если клинок долго в манекене.
9. Меч в стойке виден в правой нижней части экрана и не исчезает/не обрезается при замахах.

## Что должен проверить локальный агент
1. **Компиляция**: пакет писался без доступа к проекту. Нужен пакет HitFeel (namespace `EggGame.HitFeel`).
   HitFeel мог быть изменён в проекте — проверить, что остались публичные: `HitFeelController.PlayHit(HitFeelProfile, HitInfo, Animator)`,
   `PlaySwingStart(HitFeelProfile)`, поля `profile` и `weaponAnimator`, `KnockbackReceiver`, конструктор `HitInfo(5 параметров)`
   и поля `HitFeelProfile` (hitStopSeconds, knockbackDistance, knockbackUp, shake*, lunge* и т.д.).
   Если что-то переименовано — поправить в `ComboHitFeelBridge.cs` и `SwordComboController.cs`, НЕ в HitFeel.
   Если в проекте есть `.asmdef`, папкам HitFeel и SwordCombo нужна ссылка на `Unity.InputSystem` (если он используется).
2. **Путь урона**: в `LocalDamageSender.SendDamageRequest` (место помечено `>>> МЕСТО ПОДКЛЮЧЕНИЯ <<<`)
   заменить заглушку `IDamageable` на существующую систему урона проекта. Для Fusion 2 — написать свой
   компонент с `IDamageRequestSender`, который шлёт RPC хосту (`DamageRequest` содержит всё нужное),
   а хост проверяет дистанцию/кулдаун и наносит урон. Эффекты (`OnImpact`) уже отделены и остаются локальными.
3. **Ввод**: убедиться, что клик не конфликтует с другим действием на ЛКМ (UI, стрельба, строительство);
   при необходимости выключить *Read Mouse Input* и вызывать `RequestAttack()` из своего ввода.
4. **Камера**: `CameraShake` и `SwingCameraSway` стоят на своих отдельных объектах над камерой и активны только у локального игрока;
   `SwordPivot` — дочерний объект камеры; Near Clip Plane ≈ 0.05.
5. **Сеть**: у чужих игроков `SwordComboController.isLocalPlayer = false` и `HitFeelController.isLocalPlayer = false`
   (или компоненты выключены), иначе их клики/эффекты сработают у вас.
6. **Слои**: *Hit Layers* не включает слой игрока; у врагов есть не-триггер коллайдеры.
7. **Старый MeleeHitbox** (если стоял) выключен, чтобы урон не считался дважды.
