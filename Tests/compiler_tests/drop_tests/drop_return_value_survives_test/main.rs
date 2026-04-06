use Std.Ops.Drop;
struct Dropper {
    r: &uniq i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 100;
    }
}

fn make_and_return(r: &uniq i32) -> i32 {
    let d = make Dropper { r: r };
    42
}

fn main() -> i32 {
    let counter = 0;
    let val: i32 = make_and_return(&uniq counter);
    val
}
