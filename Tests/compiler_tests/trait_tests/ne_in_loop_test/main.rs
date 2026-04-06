fn main() -> i32 {
    let i = 0;
    let sum = 0;
    while i < 10 {
        if i != 5 {
            sum = sum + 1;
        };
        i = i + 1;
    };
    sum
}
