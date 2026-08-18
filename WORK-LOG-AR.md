# Ashenlum — شو عملنا وليش

| التقنية | وين استخدمناها | ليش |
|---|---|---|
| **Abstract class** | `ShopGood` ← Talisman / StrengthUpgrade / LumenBundle | المحل بيحمل list وحدة بس. كل نوع بيجاوب لحاله: قديش سعره، خلص من المخزن ولا لأ، وشو بيصير لما تشتريه. |
| **Abstract class** | `ActiveAbility` ← `ProjectileAbility` | بدك قدرة جديدة؟ بتعمل subclass جديد وخلص. كود اللاعب ما بينلمس. |
| **Abstract class** | `Interactable` ← المحل / الـCheckPoint / الحوار | كل إشي بتضغط عليه **E** بيرث من نفس الكلاس، فنظام الـprompt واحد للكل. |
| **Interface** | `IDamageable` | اللاعب والأعداء والـboss والـshade كلهم بياخدوا ضرر. الهجمة مش فارق معها مين ضربت. |
| **Interface** | `IRespawnReset` | لما تطفّي object بتموت الـcoroutines تبعته للأبد — فالعدو اللي مات بنص الضربة كان بيرجع والـhitbox شغال عليه. هاي بترجّع اللي الـcoroutine الميت ما لحق يرجّعه. |
| **ScriptableObject** | التمائم، القدرات، الحوارات | الداتا بتصير assets. بتعدّل التوازن من الـInspector بدون ما تفتح كود. |
| **Static registry** | `WorldReset` | العدو الميت بيكون مطفي، والـsearch مش بلاقيه — فالأعداء بيسجّلوا حالهم لحالهم. |
| **Reference counting** | `TimeManager` | البوز والمخزن والمحل والمودال كلهم فيهم يجمّدوا الوقت بنفس اللحظة. الوقت بيرجع بس لما **آخر** واحد يفلت. |
| **DTO / فصل الموديل** | `RunSave` مقابل `GameRunProfile` | الـ`JsonUtility` ما بتعرف تكتب `Dictionary` ولا `HashSet` ولا asset reference — فبنحوّلهم **ids** وقت الحفظ وبنرجّعهم assets وقت التحميل. وكمان بتخلينا نعدّل كلاس اللعبة بدون ما نكسر الحفظات القديمة. |
| **Derived state** | بونص التمائم | بينحسب من اللي لابسه هلق، مش مخزَّن. عشان هيك لما تشيل التميمة مش لازم تتذكر شو تطرح. |
| **Callbacks (`Action`)** | `ConfirmModal` | المودال ما بيعرف شو بيأكّد — بتبعتله عنوان ورسالة وfunction، وهو بيناديها إذا الشخص وافق. |
| **Events** | `OnLumensChanged` | العداد بيتحدّث لما يتغير الرقم، بدل ما يضل يسأل كل frame. |
