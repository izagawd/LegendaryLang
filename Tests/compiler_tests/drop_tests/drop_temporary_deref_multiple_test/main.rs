// Multiple temporary derefs in one scope — each anonymous local is registered
// independently and all must be dropped at scope exit. The two Sentinels
// both hold &mut to the same counter (which is valid — &mut allows aliasing),
// so dropped ends at 2 after both temporaries are dropped.
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
    let a = *make Wrapper { inner: make Sentinel { counter: &mut dropped }, val: 3 };
    let b = *make Wrapper { inner: make Sentinel { counter: &mut dropped }, val: 5 };
    // both temporaries dropped at end of main → dropped == 2
    a + b + dropped
}
