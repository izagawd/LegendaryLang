use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 1;
    }
}

fn take_dropper['a](d: Dropper['a]) -> i32 {
    0
}

fn main() -> i32 {
    let counter = 0;
    {
        let d = make Dropper { r : &mut counter };
        take_dropper(d);
    }
    counter
}
