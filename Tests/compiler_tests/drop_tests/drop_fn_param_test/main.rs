use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a uniq i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 5;
    }
}

fn use_dropper['a](d: Dropper['a]) -> i32 {
    0
}

fn main() -> i32 {
    let counter = 0;
    use_dropper(make Dropper { r : &uniq counter });
    counter
}
