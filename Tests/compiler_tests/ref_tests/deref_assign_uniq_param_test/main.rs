fn add_to(r: &mut i32, amount: i32) {
    *r = *r + amount;
}

fn main() -> i32 {
    let counter = 0;
    add_to(&mut counter, 7);
    add_to(&mut counter, 3);
    counter
}
