fn main() -> i32 {
    let i = 0;
    let sum = 0;
    while i < 5 {
        if i == 3 {
            sum = sum + 100;
        };
        sum = sum + 1;
        i = i + 1;
    };
    sum
}
