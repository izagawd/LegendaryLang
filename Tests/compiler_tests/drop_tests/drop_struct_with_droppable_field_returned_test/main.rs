use Std.Ops.Drop;
struct Dropper {
    r: &mut i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

struct Wrapper {
    inner: Dropper,
    tag: i32
}

fn make_wrapper(r: &mut i32) -> Wrapper {
    make Wrapper {
        inner: make Dropper { r: r },
        tag: 99
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let w = make_wrapper(&mut counter);
    }
    counter
}
