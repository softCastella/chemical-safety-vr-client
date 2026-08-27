# MixerRoom GeneratedPlane 깜빡임

## 증상

`Assets/Scenes/5_MixerRoom_Unlit.unity`의 `GeneratedPlane`이 Play Mode에서 번쩍이는 현상이 관찰되었다.

## 확인 내용

- 씬에 `GeneratedPlane` 오브젝트가 여러 개 있고, 동일한 안전표지용 메시/머티리얼을 공유한다.
- 해당 표지 면이 조명 그림자와 깊이 계산의 영향을 받을 수 있다.

## 적용한 완화

안전표지용 GeneratedPlane 렌더러 6개의 `Cast Shadows`와 `Receive Shadows`를 껐다. 표지판은 자체 그림자보다 안정적인 표시가 우선이므로, 이 변경은 씬에 직렬화되어 Play Mode에서도 유지된다.

## 검증

Unity에서 컴파일 후 `5_MixerRoom_Unlit`을 열고 Play Mode로 재확인해야 한다. 계속 깜빡이면 Scene 뷰에서 겹친 면(z-fighting) 또는 중복 GeneratedPlane을 확인하고, 필요한 경우 해당 면의 깊이 오프셋/중복 오브젝트를 별도로 조정한다.
