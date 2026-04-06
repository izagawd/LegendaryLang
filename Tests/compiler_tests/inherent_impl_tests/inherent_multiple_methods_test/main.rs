struct Math {
    base_val: i32
}

impl Math {
    fn add(self: &Self, n: i32) -> i32 {
        self.base_val + n
    }

    fn mul(self: &Self, n: i32) -> i32 {
        self.base_val * n
    }
}

fn main() -> i32 {
    let m = make Math { base_val : 5 };
    m.add(3) + m.mul(2)
}
