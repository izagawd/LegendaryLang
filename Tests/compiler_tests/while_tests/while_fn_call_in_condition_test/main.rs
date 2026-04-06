fn should_continue(i: i32, limit: i32) -> bool {
    i < limit
}

fn main() -> i32 {
    let i = 0;
    let sum = 0;
    while should_continue(i, 5) {
        sum = sum + i;
        i = i + 1;
    };
    sum
}
