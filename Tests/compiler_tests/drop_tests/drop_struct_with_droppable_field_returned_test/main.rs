use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

struct Wrapper['a] {
    inner: Dropper['a],
    tag: i32
}

fn make_wrapper['a](r: &'a mut i32) -> Wrapper['a] {
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
