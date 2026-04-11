struct Counter { val: i32 }
impl Counter {
    fn inc(self: &mut Self) { self.val = self.val + 1; }
    fn get(self: &Self) -> i32 { self.val }
}

fn inc_through(r: &&mut Counter) {
    r.inc();
}

fn main() -> i32 {
    let c = make Counter { val: 41 };
    let m = &mut c;
    inc_through(&m);
    c.val
}
