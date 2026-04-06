struct Holder['a] {
    r: &'a uniq i32
}

impl['a] Holder['a] {
    fn set(self: &uniq Self, val: i32) {
        *self.r = val;
    }
}

fn main() -> i32 {
    let x = 0;
    let h = make Holder { r : &uniq x };
    h.set(99);
    x
}
