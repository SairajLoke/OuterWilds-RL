

### add metrics to visualize training (see RL_MAP)



### Tweaks: 

1. pSkipFrame 

as mentioned in original DQN paper  https://arxiv.org/pdf/1312.5602
"More precisely, the agent sees and selects actions on every kth frame instead of every
frame, and its last action is repeated on skipped frames. Since running the emulator forward for one
step requires much less computation than having the agent select an actio"

as ig the state will be very similar as well, so good to skip, but how much that is the question as they menion about Space Invaders.
Even quad-ai repo uses this
