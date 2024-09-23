using System;
using System.Collections;
using System.Collections.Generic;
using Azul;
using UnityEngine;

namespace Statics {
    public struct Holding {
        public bool isHolding;
        public int typeId { get; private set; }
        public int count { get; private set; }
        public int plateId { get; private set; }

        public Holding(bool isHolding) {
            this.isHolding = isHolding;
            typeId = Globals.EMPTY_CELL;
            count = Globals.EMPTY_CELL;
            plateId = Globals.EMPTY_CELL;
        }

        public void PutToHand(int typeId, int count, int plateId) {
            isHolding = true;
            this.typeId = typeId;
            this.count = count;
            this.plateId = plateId;
        }

        public void EmptyHand() {
            isHolding = false;
            typeId = Globals.EMPTY_CELL;
            count = Globals.EMPTY_CELL;
            plateId = Globals.EMPTY_CELL;
        }

        public Vector3Int GetData() {
            if(!isHolding) throw new Exception("You dont have nothing to hold.");
            return new Vector3Int(typeId, count, plateId);
        }
    }
    
}