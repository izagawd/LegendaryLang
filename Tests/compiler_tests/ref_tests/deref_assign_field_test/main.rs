struct Holder['a] {
    r: &'a mut i32
}

impl['a] Holder['a] {
    fn set(self: &mut Self, val: i32) {
        *self.r = val;
    }
}

fn main() -> i32 {
    let x = 0;
    {
        let h = make Holder { r : &mut x };
        h.set(99);
    };
    x
}
