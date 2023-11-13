import cv2
import mediapipe as mp
import os
from os import walk
from os.path import join

mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles
mp_holistic = mp.solutions.holistic
mp_hands = mp.solutions.hands


def extractSkeleton(filePath, fileName, saveFilePath, saveFileName):
  frameCount = 0
  landmarksList = []
  landmarksList2D = []
  h = 0
  w = 0

  name = join(filePath, fileName)

  cap = cv2.VideoCapture(name)
  with mp_holistic.Holistic(
      model_complexity=2,
      min_detection_confidence=0.5,
      min_tracking_confidence=0.5) as holistic:
    with mp_hands.Hands(
      model_complexity=1,
      min_detection_confidence=0.5,
      min_tracking_confidence=0.5) as hands:
      while cap.isOpened():
        success, image = cap.read()
        if not success:
          print("Ignoring empty camera frame.")
          # If loading a video, use 'break' instead of 'continue'.
          break

        # To improve performance, optionally mark the image as not writeable to
        # pass by reference.
        image.flags.writeable = False
        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        h, w = image.shape[:2]
        results = holistic.process(image)
        resultsHands = hands.process(image)

        # Draw landmark annotation on the image.
        image.flags.writeable = True
        image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)
        mp_drawing.draw_landmarks(
            image,
            results.face_landmarks,
            mp_holistic.FACEMESH_CONTOURS,
            landmark_drawing_spec=None,
            connection_drawing_spec=mp_drawing_styles
            .get_default_face_mesh_contours_style())
        mp_drawing.draw_landmarks(
            image,
            results.pose_landmarks,
            mp_holistic.POSE_CONNECTIONS,
            landmark_drawing_spec=mp_drawing_styles
            .get_default_pose_landmarks_style())

        if resultsHands.multi_hand_landmarks:
          for hand_landmarks in resultsHands.multi_hand_landmarks:
            mp_drawing.draw_landmarks(
                image,
                hand_landmarks,
                mp_hands.HAND_CONNECTIONS,
                mp_drawing_styles.get_default_hand_landmarks_style(),
                mp_drawing_styles.get_default_hand_connections_style())

        # Flip the image horizontally for a selfie-view display.
        # cv2.imshow('MediaPipe Holistic', cv2.flip(image, 1))
        cv2.imshow('MediaPipe Holistic', image)
        # the center between hips.

        # print('Nose world landmark:')
        if (results.pose_world_landmarks is not None) or (results.pose_landmarks is not None):
          landmarksList.append(results.pose_world_landmarks.landmark)
          landmarksList2D.append(results.pose_landmarks.landmark)
          frameCount += 1
        else:
          print("skeleton not found")
          break  

        # print(results.pose_world_landmarks.landmark[mp_holistic.PoseLandmark.RIGHT_HIP])
        # print(results.pose_world_landmarks.landmark[mp_holistic.PoseLandmark.LEFT_HIP])


        if cv2.waitKey(5) & 0xFF == 27:
          break

  animationFile = open(join(saveFilePath, saveFileName), 'w')
  animationFile.write(str(frameCount) + " " + str(len(mp_holistic.PoseLandmark)) + " " + str(w) + " " + str(h) +"\n\n")

  count = 0
  for lndmrk in landmarksList:
    lndmrk2 = landmarksList2D[count]
    for jointLndmrk in mp_holistic.PoseLandmark:
      animationFile.write(str(lndmrk[jointLndmrk].x) + ' ' + str(lndmrk[jointLndmrk].y) + ' ' + str(lndmrk[jointLndmrk].z) + ' ' + str(lndmrk[jointLndmrk].visibility) + ' ' + 
                          str(lndmrk2[jointLndmrk].x) + ' ' + str(lndmrk2[jointLndmrk].y) + ' ' + str(lndmrk2[jointLndmrk].z) +"\n")
    animationFile.write("\n")
    count += 1

  animationFile.close()
  cap.release()

# Main code
videosPath = "C:\\Users\\Nigno17\\Documents\\Datasets\\DrVCoach\\train"
dataPath = ".\\DrVCoach_DS"
if not os.path.exists(dataPath):
  os.makedirs(dataPath)

for (dirpath, dirnames, filenames) in walk(videosPath):
  actualFolder = dirpath.replace(videosPath + '\\', '')
  for filename in filenames:
      if filename != None and ".mp4" in filename:
        #label = filename[16:20]
        extractSkeleton(dirpath, filename, dataPath, filename.replace('.mp4', '-' + actualFolder + '.txt'))
