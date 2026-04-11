use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 1;
    }
}

struct Multi['a] {
    a: Dropper['a],
    plain: i32,
    b: Dropper['a]
}

fn main() -> i32 {
    let counter = 0;
    {
        let m = make Multi {
            a: make Dropper { r: &mut counter },
            plain: 99,
            b: make Dropper { r: &mut counter }
        };
    }
    counter
}
