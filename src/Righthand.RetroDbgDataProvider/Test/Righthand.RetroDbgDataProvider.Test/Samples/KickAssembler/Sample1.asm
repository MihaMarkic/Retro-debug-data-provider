        *=$1000 "Program"
        ldx #10
!loop:  dex
        bne !loop-
        rts

        *=$4000 "Data"
        .byte 1,0,2,0,3,0,4,0

        *=$5000 "More data"
        .text "Hello"