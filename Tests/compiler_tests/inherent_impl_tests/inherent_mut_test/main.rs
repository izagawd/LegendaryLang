struct Counter {
    val: i32
}

impl Counter {
    fn inc(self: &mut Self) {
        self.val = self.val + 1;
    }

    fn get(self: &Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let c = make Counter { val : 10 };
    c.inc();
    c.get()
}
