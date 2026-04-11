use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 10;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let d = make Dropper { r : &mut counter };
        let x = 5;
    }
    counter
}
