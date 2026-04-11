use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 5;
    }
}

fn use_dropper['a](d: Dropper['a]) -> i32 {
    0
}

fn main() -> i32 {
    let counter = 0;
    use_dropper(make Dropper { r : &mut counter });
    counter
}
