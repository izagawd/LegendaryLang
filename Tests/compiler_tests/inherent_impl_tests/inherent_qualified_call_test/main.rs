struct Bar {
    val: i32
}

impl Bar {
    fn extract(self: &Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let b = make Bar { val : 77 };
    Bar.extract(&b)
}
