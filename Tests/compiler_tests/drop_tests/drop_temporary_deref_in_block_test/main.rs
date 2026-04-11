// The temporary Gc created by *Gc.New(x) inside an inner block must be
// dropped when that block exits, not at end of main. We verify via a
// side-channel counter that drop has already fired by the time we reach
// the addition after the block.
use Std.Ops.Drop;
use Std.Deref.Receiver;
use Std.Deref.Deref;

struct Sentinel['a] {
    counter: &'a mut i32
}

impl['a] Drop for Sentinel['a] {
    fn Drop(self: &mut Self) {
        *self.counter = *self.counter + 1;
    }
}

struct Wrapper['a] {
    inner: Sentinel['a],
    val: i32
}

impl['a] Receiver for Wrapper['a] {
    let Target :! type = i32;
}

impl['a] Deref for Wrapper['a] {
    fn deref(self: &Self) -> &i32 {
        &self.val
    }
}

fn main() -> i32 {
    let dropped = 0;
    let val = {
        *make Wrapper { inner: make Sentinel { counter: &mut dropped }, val: 10 }
    };
    // Wrapper (and its Sentinel field) dropped at inner block exit → dropped == 1
    val + dropped
}
