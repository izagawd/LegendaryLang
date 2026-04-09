fn add_to(r: &uniq i32, amount: i32) {
    *r = *r + amount;
}

fn main() -> i32 {
    let counter = 0;
    add_to(&uniq counter, 7);
    add_to(&uniq counter, 3);
    counter
}
