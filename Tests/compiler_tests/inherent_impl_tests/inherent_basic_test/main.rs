struct Counter {
    val: i32
}

impl Counter {
    fn get(self: &Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let c = make Counter { val : 42 };
    c.get()
}
