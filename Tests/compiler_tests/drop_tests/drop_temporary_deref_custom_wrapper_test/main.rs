// A non-Copy smart pointer that wraps a Box internally and signals its drop
// via a mutation side-channel. This verifies that when *Wrapper.New(&mut counter, 7)
// is used as a temporary, Wrapper's Drop impl fires and increments counter,
// proving the temporary was properly dropped.
use Std.Ops.Drop;
use Std.Deref.Receiver;
use Std.Deref.Deref;
use Std.Deref.Receiver;
use Std.Deref.Deref;

struct Wrapper['a] {
    inner: Box(i32),
    dropped: &'a mut i32
}

impl['a] Wrapper['a] {
    fn New(dropped: &'a mut i32, val: i32) -> Wrapper['a] {
        make Wrapper { inner: Box.New(val), dropped: dropped }
    }
}

impl['a] Receiver for Wrapper['a] {
    let Target :! type = i32;
}

impl['a] Deref for Wrapper['a] {
    fn deref(self: &Self) -> &i32 {
        &*self.inner
    }
}

impl['a] Drop for Wrapper['a] {
    fn Drop(self: &uniq Self) {
        *self.dropped = *self.dropped + 1;
    }
}

fn main() -> i32 {
    let dropped = 0;
    // *Wrapper.New(..., 7) is a temporary — value 7 is read, then drop must fire
    let val = *Wrapper.New(&mut dropped, 7);
    // drop fired → dropped == 1; val == 7; result == 8
    val + dropped
}
