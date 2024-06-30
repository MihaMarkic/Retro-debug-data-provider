        // Uses zeropage form of lda and sta eventhough the labels is first 
        // resolved later
        lda zpReg1
        sta zpReg2

*=$10 virtual 
.zp {
zpReg1: .byte 0
zpReg2: .byte 0
}