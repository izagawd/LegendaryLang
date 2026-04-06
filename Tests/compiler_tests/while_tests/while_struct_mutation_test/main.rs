struct Counter {
    val: i32
}
impl Copy for Counter {}

fn main() -> i32 {
    let c = make Counter { val: 0 };
    while c.val < 5 {
        c.val = c.val + 1;
    };
    c.val
}
